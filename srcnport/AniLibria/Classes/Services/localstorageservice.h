#ifndef LOCALSTORAGESERVICE_H
#define LOCALSTORAGESERVICE_H

#include <QObject>
#include <QSqlDatabase>
#include "../Models/onlinevideomodel.h"
#include "../Models/releasemodel.h"
#include "../Models/releasetorrentmodel.h"

class LocalStorageService : public QObject
{
    Q_OBJECT

private:
    QSqlDatabase m_Database;
    QFutureWatcher<void>* m_AllReleaseUpdatedWatcher;

    QString videosToJson(QList<OnlineVideoModel>& videos);
    QString torrentsToJson(QList<ReleaseTorrentModel>& torrents);
    bool isReleaseExists(int id);
    void insertRelease(ReleaseModel& releaseModel);
    void updateRelease(ReleaseModel& releaseModel);
    QStringList getAllFavorites();
    QMap<int, int> getScheduleAsMap();
    bool checkOrCondition(QStringList source, QStringList target);
    bool checkAllCondition(QStringList source, QStringList target);
    void removeTrimsInStringCollection(QStringList list);

public:
    explicit LocalStorageService(QObject *parent = nullptr);
    ~LocalStorageService();

    Q_INVOKABLE void updateAllReleases(const QString& releases);
    Q_INVOKABLE QString getRelease(int id);
    Q_INVOKABLE QString getReleasesByFilter(int page, QString title, int section, QString description, QString type, QString genres, bool genresOr, QString voices, bool voicesOr);
    Q_INVOKABLE void setSchedule(QString schedule);
    Q_INVOKABLE QString getSchedule();
    Q_INVOKABLE void updateFavorites(QString data);
    Q_INVOKABLE QList<int> getFavorites();

signals:
    void allReleasesFinished();

public slots:
    void allReleasesUpdated();

};

#endif // LOCALSTORAGESERVICE_H
