#include <QGuiApplication>
#include <QQmlApplicationEngine>
#include "Classes/Services/synchronizationservice.h"
#include "Classes/Models/releaseitemmodel.h"

int main(int argc, char *argv[])
{
    QCoreApplication::setAttribute(Qt::AA_EnableHighDpiScaling);

    QGuiApplication app(argc, argv);

    qmlRegisterType<SynchronizationService>("Anilibria.Services", 1, 0, "SynchronizationService");
    qmlRegisterType<ReleaseItemModel>("Anilibria.Models", 1, 0, "ReleaseModel");

    QQmlApplicationEngine engine;
    const QUrl url(QStringLiteral("qrc:/main.qml"));
    QObject::connect(&engine, &QQmlApplicationEngine::objectCreated,
                     &app, [url](QObject *obj, const QUrl &objUrl) {
        if (!obj && url == objUrl)
            QCoreApplication::exit(-1);
    }, Qt::QueuedConnection);
    engine.load(url);

    return app.exec();
}
