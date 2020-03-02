#include "localstorageservice.h"
#include <QStandardPaths>
#include <QVariant>
#include <QJsonDocument>
#include <QJsonObject>
#include <QJsonValue>
#include <QtConcurrent>
#include <QFuture>
#include <QDebug>
#include <QDir>
#include <QFutureWatcher>
#include <QDateTime>
#include "../Models/releasemodel.h"
#include "../Models/fullreleasemodel.h"
#include "../Models/changesmodel.h"

using namespace std;

const int FavoriteSection = 1;
const int ScheduleSection = 5;

LocalStorageService::LocalStorageService(QObject *parent) : QObject(parent),
    m_CachedReleases(new QList<FullReleaseModel>()),
    m_ChangesModel(new ChangesModel())
{
    m_AllReleaseUpdatedWatcher = new QFutureWatcher<void>(this);

    QDir cacheDicrectory(QStandardPaths::writableLocation(QStandardPaths::AppLocalDataLocation));
    QDir().mkpath(QStandardPaths::writableLocation(QStandardPaths::AppLocalDataLocation));

    createIfNotExistsFile(getReleasesCachePath(), "[]");
    createIfNotExistsFile(getScheduleCachePath(), "{}");
    createIfNotExistsFile(getFavoritesCachePath(), "[]");
    createIfNotExistsFile(getSeensCachePath(), "[]");
    createIfNotExistsFile(getNotificationCachePath(), "{ \"newReleases\": [], \"newOnlineSeries\": [], \"newTorrents\": [], \"newTorrentSeries\": {} }");
    QString favoritespath = getFavoritesCachePath();

    updateReleasesInnerCache();

    auto changesJson = getChanges();
    m_ChangesModel->fromJson(changesJson);

    connect(m_AllReleaseUpdatedWatcher, SIGNAL(finished()), this, SLOT(allReleasesUpdated()));
}

void LocalStorageService::updateAllReleases(const QString &releases)
{
    QFuture<void> future = QtConcurrent::run(
        [=] {
            QJsonParseError jsonError;
            QJsonDocument jsonDocument = QJsonDocument::fromJson(releases.toUtf8(), &jsonError);
            if (jsonError.error != 0) return; //TODO: handle this situation and show message

            auto jsonReleases = jsonDocument.array();
            auto variantList = jsonReleases.toVariantList();
            QStringList newReleasesIds;

            foreach (QJsonValue jsonRelease, jsonReleases) {
                ReleaseModel releaseModel;
                releaseModel.readFromApiModel(jsonRelease.toObject());

                FullReleaseModel currentReleaseCacheModel = getReleaseFromCache(releaseModel.id());

                FullReleaseModel newReleaseModel = mapToFullReleaseModel(releaseModel);

                if (currentReleaseCacheModel.id() > -1) {
                    if (currentReleaseCacheModel.countOnlineVideos() != newReleaseModel.countOnlineVideos()) {
                        setReleaseOnlineSeries(newReleaseModel.id(), currentReleaseCacheModel.countOnlineVideos());
                    }
                    if (currentReleaseCacheModel.countTorrents() != newReleaseModel.countTorrents()) {
                        setNewTorrents(newReleaseModel.id(), currentReleaseCacheModel.countTorrents());
                    }
                    m_CachedReleases->removeOne(currentReleaseCacheModel);

                } else {
                    newReleasesIds.append(QString::number(newReleaseModel.id()));
                }

                m_CachedReleases->append(newReleaseModel);
            }

            saveCachedReleasesToFile();
            updateReleasesInnerCache();
            saveChanges();
        }
    );
    m_AllReleaseUpdatedWatcher->setFuture(future);
}

QString LocalStorageService::videosToJson(QList<OnlineVideoModel> &videos)
{
    QJsonArray videosArray;
    foreach (auto video, videos) {
        QJsonObject jsonObject;
        video.writeToJson(jsonObject);
        videosArray.append(jsonObject);
    }
    QJsonDocument videoDocument(videosArray);
    QString videosJson(videoDocument.toJson());
    return videosJson;
}

QString LocalStorageService::torrentsToJson(QList<ReleaseTorrentModel> &torrents)
{
    QJsonArray torrentsArray;
    foreach (auto torrent, torrents) {
        QJsonObject jsonObject;
        torrent.writeToJson(jsonObject);
        torrentsArray.append(jsonObject);
    }
    QJsonDocument torrentDocument(torrentsArray);
    QString torrentJson(torrentDocument.toJson());
    return torrentJson;
}

FullReleaseModel LocalStorageService::getReleaseFromCache(int id)
{
    foreach (auto cacheRelease, *m_CachedReleases) if (cacheRelease.id() == id) return cacheRelease;

    FullReleaseModel nullObject;
    nullObject.setId(-1);
    return nullObject;
}

FullReleaseModel LocalStorageService::mapToFullReleaseModel(ReleaseModel &releaseModel)
{
    FullReleaseModel model;

    auto torrents = releaseModel.torrents();
    auto torrentJson = torrentsToJson(torrents);

    auto videos = releaseModel.videos();
    auto videosJson = videosToJson(videos);

    auto voices = releaseModel.voices().join(", ");
    if (voices.length() == 0) voices = "Не указано";

    auto genres = releaseModel.genres().join(", ");
    if (genres.length() == 0) genres = "Не указано";

    model.setId(releaseModel.id());
    model.setTitle(releaseModel.title());
    model.setCode(releaseModel.code());
    model.setOriginalName(releaseModel.names().last());
    model.setRating(releaseModel.rating());
    model.setSeries(releaseModel.series());
    model.setStatus(releaseModel.status());
    model.setType(releaseModel.type());
    model.setTimestamp(releaseModel.timestamp().toInt());
    model.setYear(releaseModel.year());
    model.setSeason(releaseModel.season());
    model.setCountTorrents(torrents.count());
    model.setCountOnlineVideos(videos.count());
    model.setDescription(releaseModel.description());
    model.setAnnounce(releaseModel.announce());
    model.setVoicers(voices);
    model.setGenres(genres);
    model.setVideos(videosJson);
    model.setTorrents(torrentJson);
    model.setPoster(releaseModel.poster());

    return model;
}

void LocalStorageService::saveCachedReleasesToFile()
{
    QJsonArray releasesArray;
    foreach (auto release, *m_CachedReleases) {
        QJsonObject jsonObject;
        release.writeToJson(jsonObject);
        releasesArray.append(jsonObject);
    }
    QJsonDocument document(releasesArray);

    QFile file(getReleasesCachePath());
    file.open(QFile::WriteOnly | QFile::Text | QFile::Truncate);
    file.write(document.toJson());
    file.close();
}

QStringList LocalStorageService::getAllFavorites()
{
    QFile favoritesCacheFile(getFavoritesCachePath());
    favoritesCacheFile.open(QFile::ReadOnly | QFile::Text);
    QString favoritesJson = favoritesCacheFile.readAll();
    favoritesCacheFile.close();

    QJsonParseError jsonError;
    QJsonDocument jsonDocument = QJsonDocument::fromJson(favoritesJson.toUtf8(), &jsonError);
    auto favorites = jsonDocument.array();
    QStringList result;
    foreach (auto favorite, favorites) result.append(QString::number(favorite.toInt()));

    return result;
}

QMap<int, int> LocalStorageService::getScheduleAsMap()
{
    QFile scheduleCacheFile(getScheduleCachePath());
    scheduleCacheFile.open(QFile::ReadOnly | QFile::Text);
    QString scheduleJson = scheduleCacheFile.readAll();
    scheduleCacheFile.close();

    QJsonParseError jsonError;
    QJsonDocument jsonDocument = QJsonDocument::fromJson(scheduleJson.toUtf8(), &jsonError);
    auto schedule = jsonDocument.object();
    auto keys = schedule.keys();
    QMap<int, int> result;
    foreach (auto key, keys) {
        auto scheduleDay = schedule.value(key).toString();
        result[key.toInt()] = scheduleDay.toInt();
    }

    return result;
}

bool LocalStorageService::checkOrCondition(QStringList source, QStringList target)
{
    foreach(QString sourceItem, source) {
        if (target.filter(sourceItem, Qt::CaseInsensitive).count() > 0) return true;
    }

    return false;
}

bool LocalStorageService::checkAllCondition(QStringList source, QStringList target)
{
    int counter = 0;
    foreach(QString sourceItem, source) {
        if (target.filter(sourceItem, Qt::CaseInsensitive).count() > 0) counter++;
    }

    return counter == source.count();
}

void LocalStorageService::removeTrimsInStringCollection(QStringList& list) {
    QMutableStringListIterator iterator(list);
    while (iterator.hasNext()) {
        QString value = iterator.next();
        iterator.setValue(value.trimmed());
    }
}

int LocalStorageService::randomBetween(int low, int high, uint seed)
{
    qsrand(seed);
    return (qrand() % ((high + 1) - low) + low);
}

QString LocalStorageService::getReleasesCachePath() const
{
    return QStandardPaths::writableLocation(QStandardPaths::AppLocalDataLocation) + "/releases.cache";
}

QString LocalStorageService::getFavoritesCachePath() const
{
    return QStandardPaths::writableLocation(QStandardPaths::AppLocalDataLocation) + "/favorites.cache";
}

QString LocalStorageService::getScheduleCachePath() const
{
    return QStandardPaths::writableLocation(QStandardPaths::AppLocalDataLocation) + "/schedule.cache";
}

QString LocalStorageService::getSeensCachePath() const
{
    return QStandardPaths::writableLocation(QStandardPaths::AppLocalDataLocation) + "/seen.cache";
}

QString LocalStorageService::getNotificationCachePath() const
{
    return QStandardPaths::writableLocation(QStandardPaths::AppLocalDataLocation) + "/notification.cache";
}

void LocalStorageService::createIfNotExistsFile(QString path, QString defaultContent)
{
    if (!QFile::exists(path)) {
        QFile createReleasesCacheFile(path);
        createReleasesCacheFile.open(QFile::WriteOnly | QFile::Text);
        createReleasesCacheFile.write(defaultContent.toUtf8());
        createReleasesCacheFile.close();
    }
}

void LocalStorageService::addNewReleases(QStringList releases)
{
    auto newReleases = m_ChangesModel->newReleases();
    foreach(auto release,  releases) {
        auto releaseId = release.toInt();

        if (!newReleases.contains(releaseId)) newReleases.append(releaseId);
    }
}

void LocalStorageService::setReleaseOnlineSeries(int releaseId, int count)
{
    auto onlineSeries = m_ChangesModel->newOnlineSeries();
    if (!onlineSeries.contains(releaseId)) {
        onlineSeries.insert(releaseId, count);
    } else {
        onlineSeries[releaseId] = count;
    }
}

void LocalStorageService::setNewTorrents(int releaseId, int count)
{
    auto torrents = m_ChangesModel->newTorrents();
    if (!torrents.contains(releaseId)) {
        torrents.insert(releaseId, count);
    } else {
        torrents[releaseId] = count;
    }
}

void LocalStorageService::saveChanges()
{
    QFile notificationFile(getNotificationCachePath());
    if (!notificationFile.open(QFile::WriteOnly | QFile::Text)) {
        //TODO: handle this situation
    }
    notificationFile.write(m_ChangesModel->toJson().toUtf8());
    notificationFile.close();
}

QString LocalStorageService::getRelease(int id)
{
    QListIterator<FullReleaseModel> i(*m_CachedReleases);

    while(i.hasNext()) {
        auto release = i.next();
        if (release.id() == id) {
            QJsonObject jsonValue;
            release.writeToJson(jsonValue);

            QJsonDocument saveDoc(jsonValue);
            return saveDoc.toJson();
        }
    }

    return "{}";
}

QString LocalStorageService::getRandomRelease()
{
    auto count = m_CachedReleases->count() - 1;

    auto position = randomBetween(1, count, static_cast<uint>(QDateTime::currentMSecsSinceEpoch()));

    auto release = m_CachedReleases->at(position);

    QJsonObject jsonValue;
    release.writeToJson(jsonValue);

    QJsonDocument saveDoc(jsonValue);
    return saveDoc.toJson();
}

QString LocalStorageService::getChanges()
{
    QFile notificationFile(getNotificationCachePath());
    if (!notificationFile.open(QFile::ReadOnly | QFile::Text)) {
        //TODO: handle this situation
    }
    auto changes = notificationFile.readAll();
    notificationFile.close();

    return changes;
}

static bool compareTimeStamp(const FullReleaseModel& first, const FullReleaseModel& second)
{
    return first.timestamp() < second.timestamp();
}

static bool compareTimeStampDescending(const FullReleaseModel& first, const FullReleaseModel& second)
{
    return first.timestamp() > second.timestamp();
}

static bool compareName(const FullReleaseModel& first, const FullReleaseModel& second)
{
    return first.title() < second.title();
}

static bool compareNameDescending(const FullReleaseModel& first, const FullReleaseModel& second)
{
    return first.title() > second.title();
}

static bool compareYear(const FullReleaseModel& first, const FullReleaseModel& second)
{
    return first.year() < second.year();
}

static bool compareYearDescending(const FullReleaseModel& first, const FullReleaseModel& second)
{
    return first.year() > second.year();
}

static bool compareRating(const FullReleaseModel& first, const FullReleaseModel& second)
{
    return first.rating() < second.rating();
}

static bool compareRatingDescending(const FullReleaseModel& first, const FullReleaseModel& second)
{
    return first.rating() > second.rating();
}

static bool compareStatus(const FullReleaseModel& first, const FullReleaseModel& second)
{
    return first.status() < second.status();
}

static bool compareStatusDescending(const FullReleaseModel& first, const FullReleaseModel& second)
{
    return first.status() > second.status();
}

static bool compareOriginalName(const FullReleaseModel& first, const FullReleaseModel& second)
{
    return first.originalName() < second.originalName();
}

static bool compareOriginalNameDescending(const FullReleaseModel& first, const FullReleaseModel& second)
{
    return first.originalName() > second.originalName();
}

static bool compareSeason(const FullReleaseModel& first, const FullReleaseModel& second)
{
    return first.season() < second.season();
}

static bool compareSeasonDescending(const FullReleaseModel& first, const FullReleaseModel& second)
{
    return first.season() > second.season();
}

QString LocalStorageService::getReleasesByFilter(int page, QString title, int section, QString description, QString type, QString genres, bool genresOr, QString voices, bool voicesOr, QString years, QString seasones, QString statuses, int sortingField, bool sortingDescending)
{
    int pageSize = 12;
    int startIndex = (page - 1) * pageSize;

    QStringList userFavorites = getAllFavorites();
    QMap<int, int> scheduled = getScheduleAsMap();

    std::function<bool (const FullReleaseModel&, const FullReleaseModel&)> scheduleComparer = [scheduled](const FullReleaseModel& first, const FullReleaseModel& second) {
        auto firstId = first.id();
        auto firstScheduled = scheduled.contains(firstId) ? scheduled[firstId] : 9;

        auto secondId = second.id();
        auto secondScheduled = scheduled.contains(secondId) ? scheduled[secondId] : 9;

        return firstScheduled < secondScheduled;
    };

    std::function<bool (const FullReleaseModel&, const FullReleaseModel&)> scheduleDescendingComparer = [scheduled](const FullReleaseModel& first, const FullReleaseModel& second) {
        auto firstId = first.id();
        auto firstScheduled = scheduled.contains(firstId) ? scheduled[firstId] : 9;

        auto secondId = second.id();
        auto secondScheduled = scheduled.contains(secondId) ? scheduled[secondId] : 9;

        return firstScheduled > secondScheduled;
    };

    QJsonArray releases;

    switch (sortingField) {
        case 0:
            std::sort(m_CachedReleases->begin(), m_CachedReleases->end(), sortingDescending ? compareTimeStampDescending : compareTimeStamp);
            break;
        case 1: //Дню в расписании
            std::sort(m_CachedReleases->begin(), m_CachedReleases->end(), sortingDescending ? scheduleDescendingComparer : scheduleComparer);
            break;
        case 2: //Имени
            std::sort(m_CachedReleases->begin(), m_CachedReleases->end(), sortingDescending ? compareNameDescending : compareName);
            break;
        case 3: //Году
            std::sort(m_CachedReleases->begin(), m_CachedReleases->end(), sortingDescending ? compareYearDescending : compareYear);
            break;
        case 4: //Рейтингу
            std::sort(m_CachedReleases->begin(), m_CachedReleases->end(), sortingDescending ? compareRatingDescending : compareRating);
            break;
        case 5: //Статусу
            std::sort(m_CachedReleases->begin(), m_CachedReleases->end(), sortingDescending ? compareStatusDescending : compareStatus);
            break;
        case 6: //Оригинальному имени
            std::sort(m_CachedReleases->begin(), m_CachedReleases->end(), sortingDescending ? compareOriginalNameDescending : compareOriginalName);
            break;
        case 7: //История
            //std::sort(m_CachedReleases->begin(), m_CachedReleases->end(), sortingDescending ? compareOriginalNameDescending : compareOriginalName);
            break;
        case 8: //История просмотра
            //std::sort(m_CachedReleases->begin(), m_CachedReleases->end(), sortingDescending ? compareOriginalNameDescending : compareOriginalName);
            break;
        case 9: //Сезону
            std::sort(m_CachedReleases->begin(), m_CachedReleases->end(), sortingDescending ? compareSeasonDescending : compareSeason);
            break;
    }

    foreach (auto releaseItem, *m_CachedReleases) {

        if (!title.isEmpty() && !releaseItem.title().toLower().contains(title.toLower())) continue;
        if (!description.isEmpty() && !releaseItem.description().toLower().contains(description.toLower())) continue;
        if (!type.isEmpty() && !releaseItem.type().toLower().contains(type.toLower())) continue;

        //years
        if (!years.isEmpty()) {
            QStringList yearsList = years.split(",");
            removeTrimsInStringCollection(yearsList);
            int year = releaseItem.year().toInt();
            QStringList singleYear;
            singleYear.append(QString::number(year));

            if (!checkOrCondition(yearsList, singleYear)) continue;
        }

        //statuses
        if (!statuses.isEmpty()) {
            QStringList statusesList = statuses.split(",");
            removeTrimsInStringCollection(statusesList);
            QStringList singleStatus;
            singleStatus.append(releaseItem.status());

            if (!checkOrCondition(statusesList, singleStatus)) continue;
        }

        //seasons
        if (!seasones.isEmpty()) {
            QStringList seasonesList = seasones.split(",");
            removeTrimsInStringCollection(seasonesList);
            auto season = releaseItem.season();
            QStringList singleSeason;
            singleSeason.append(season);

            if (!checkOrCondition(seasonesList, singleSeason)) continue;
        }

        //genres
        if (!genres.isEmpty()) {
            QStringList genresList = genres.split(",");
            removeTrimsInStringCollection(genresList);
            QStringList releaseGenresList = releaseItem.genres().split(",");
            if (genresOr) {
                if (!checkAllCondition(genresList, releaseGenresList)) continue;
            } else {
                if (!checkOrCondition(genresList, releaseGenresList)) continue;
            }
        }

        //voices
        if (!voices.isEmpty()) {
            QStringList voicesList = voices.split(",");
            QStringList releaseVoicesList = releaseItem.voicers().split(",");
            if (voicesOr) {
                if (!checkAllCondition(voicesList, releaseVoicesList)) continue;
            } else {
                if (!checkOrCondition(voicesList, releaseVoicesList)) continue;
            }
        }

        //favorites section
        if (section == FavoriteSection) {
            auto releaseId = releaseItem.id();
            if (!userFavorites.contains(QString::number(releaseId))) continue;
        }

        if (section == ScheduleSection && !scheduled.contains(releaseItem.id())) continue;

        if (startIndex > 0) {
            startIndex--;
            continue;
        }

        QJsonObject jsonValue;
        releaseItem.writeToJson(jsonValue);
        releases.append(jsonValue);

        if (releases.count() >= pageSize) break;
    }

    QJsonDocument saveDoc(releases);
    return saveDoc.toJson();
}

void LocalStorageService::setSchedule(QString schedule)
{
    QFile scheduleCacheFile(getScheduleCachePath());
    scheduleCacheFile.open(QFile::WriteOnly | QFile::Text);
    scheduleCacheFile.write(schedule.toUtf8());
    scheduleCacheFile.close();
}

QString LocalStorageService::getSchedule()
{
    QFile scheduleCacheFile(getScheduleCachePath());
    scheduleCacheFile.open(QFile::ReadOnly | QFile::Text);
    QString scheduleJson = scheduleCacheFile.readAll();
    scheduleCacheFile.close();
    return scheduleJson;
}

void LocalStorageService::updateFavorites(QString data)
{
    QFile favoritesCacheFile(getFavoritesCachePath());
    favoritesCacheFile.open(QFile::WriteOnly | QFile::Text);
    favoritesCacheFile.write(data.toUtf8());
    favoritesCacheFile.close();
}

QList<int> LocalStorageService::getFavorites()
{
    auto favorites = getAllFavorites();
    QList<int> ids;
    foreach(auto favorite, favorites) ids.append(favorite.toInt());

    return ids;
}

void LocalStorageService::clearFavorites()
{
    updateFavorites("[]");
}

void LocalStorageService::updateReleasesInnerCache()
{
    m_CachedReleases->clear();

    QFile releasesCacheFile(getReleasesCachePath());

    releasesCacheFile.open(QFile::ReadOnly | QFile::Text);

    QString releasesJson = releasesCacheFile.readAll();
    releasesCacheFile.close();
    auto releasesArray = QJsonDocument::fromJson(releasesJson.toUtf8()).array();

    foreach (auto release, releasesArray) {
        FullReleaseModel jsonRelease;
        jsonRelease.readFromJson(release);

        m_CachedReleases->append(jsonRelease);
    }
}

void LocalStorageService::allReleasesUpdated()
{
    emit allReleasesFinished();
}
