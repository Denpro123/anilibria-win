#ifndef RELEASEMODEL_H
#define RELEASEMODEL_H

#include <QtCore>

class ReleaseModel
{
private:
    int m_Id;
    QString m_Code;
    QString m_Series;
    QString m_Poster;
    QString m_Timestamp;
    QString m_Status;
    QString m_Type;
    QString m_Year;
    QString m_Description;
    QStringList m_Genres;
    QStringList m_Voices;
    QStringList m_Names;
    QString m_Title;
    QString m_Season;
    int m_Rating;

public:
    ReleaseModel();

    void readFromApiModel(const QJsonObject &jsonObject);

    void writeToJson(QJsonObject &json) const;

    void readFromJson(const QJsonObject &json);

    QString code();

    QString series();

    QString poster();

    QString timestamp();

    QString status();

    QString type();

    QString year();

    QString description();

    QString season();

    QStringList genres();

    QStringList voices();

    QStringList names();

    int rating();

    QString title();

};

#endif // RELEASEMODEL_H
