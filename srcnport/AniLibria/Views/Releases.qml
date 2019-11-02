import QtQuick 2.12
import QtQuick.Controls 2.5
import QtQuick.Layouts 1.3
import QtWebView 1.1
import QtQuick.Controls.Styles 1.4
import QtGraphicalEffects 1.0
import Anilibria.Services 1.0
import "../Controls"

Page {
    id: page
    property bool selectMode
    property var selectedReleases: []
    property bool isBusy: false
    property var openedRelease: null

    signal navigateFrom()
    signal watchRelease(int releaseId)

    onWidthChanged: {
        const columnCount = parseInt(page.width / 520);
        itemGrid.columns = columnCount < 1 ? 1 : columnCount;
    }

    background: Rectangle {
        color: "#D3D3D3"
    }

    anchors.fill: parent

    Rectangle {
        id: mask
        width: 180
        height: 260
        radius: 10
        visible: false
    }

    Rectangle {
        id: cardMask
        width: 180
        height: 260
        radius: 6
        visible: false
    }

    RowLayout {
        id: panelContainer
        anchors.fill: parent
        spacing: 0
        Rectangle {
            color: "#9e2323"
            width: 40
            Layout.fillHeight: true
            Column {
                IconButton {
                    height: 45
                    width: 40
                    iconColor: "white"
                    iconPath: "../Assets/Icons/menu.svg"
                    iconWidth: 29
                    iconHeight: 29
                    onButtonPressed: {
                        drawer.open();
                    }
                }
            }
        }

        ColumnLayout {
            Layout.fillHeight: true
            Layout.fillWidth: true
            spacing: 2

            Rectangle {
                Layout.fillWidth: true
                Layout.preferredHeight: 45
                height: 45
                color: "#808080"
                Switch {
                    onCheckedChanged: {
                        page.selectMode = checked;
                        if (!checked) {
                            for (const selectedRelease of page.selectedReleases) {
                                selectedRelease.selected = false;
                            }
                            page.selectedReleases = [];
                        } else {
                            page.openedRelease = null;
                        }
                    }
                }
            }

            Rectangle {
                id: filtersContainer
                Layout.preferredWidth: 240
                Layout.alignment: Qt.AlignHCenter
                Layout.preferredHeight: 44
                color: "transparent"

                RoundedTextBox {
                    width: filtersContainer.width
                    height: 30
                    textContent: ""
                    fontSize: 16
                    placeholder: "Введите название релиза"
                }
            }

            Flickable {
                id: scrollview
                Layout.fillHeight: true
                Layout.fillWidth: true
                Layout.alignment: Qt.AlignJustify
                clip: true
                contentWidth: parent.width
                contentHeight: itemGrid.height
                onContentYChanged: {
                    if (scrollview.atYEnd && !page.isBusy) {
                        page.isBusy = true;
                        releasesService.fillNextReleases();
                        page.isBusy = false;
                    }
                }
                ScrollBar.vertical: ScrollBar {
                    active: true
                }

                ColumnLayout {
                    width: page.width
                    height: page.height
                    Grid {
                        id: itemGrid
                        Layout.alignment: Qt.AlignHCenter
                        columns: 2
                        spacing: 4
                        //width: 540
                        Repeater {
                            model: releasesService.releases
                            Rectangle {
                                width: 480
                                height: 260
                                radius: 10
                                border.color: "red"
                                border.width: modelData.selected ? 3 : 0
                                color: "#f2f2f2"
                                MouseArea {
                                    width: 480
                                    height: 260
                                    onClicked: {
                                        if (page.openedRelease) return;

                                        page.selectItem(modelData);
                                    }
                                }
                                Grid {
                                    columnSpacing: 3
                                    columns: 2
                                    bottomPadding: 4
                                    leftPadding: 4
                                    topPadding: 4
                                    rightPadding: 4
                                    Image {
                                        source: modelData.poster
                                        fillMode: Image.PreserveAspectCrop
                                        width: 180
                                        height: 252
                                        layer.enabled: true
                                        layer.effect: OpacityMask {
                                            maskSource: mask
                                        }
                                    }
                                    Column {
                                        Text {
                                            textFormat: Text.RichText
                                            color: "#a32727"
                                            font.pointSize: 12
                                            width: 280
                                            leftPadding: 8
                                            topPadding: 6
                                            wrapMode: Text.WordWrap
                                            maximumLineCount: 3
                                            text: qsTr(modelData.title)
                                        }
                                        Text {
                                            textFormat: Text.RichText
                                            font.pointSize: 10
                                            leftPadding: 8
                                            topPadding: 4
                                            text: qsTr("<b>Статус:</b> ") + qsTr(modelData.status)
                                        }
                                        Text {
                                            font.pointSize: 10
                                            leftPadding: 8
                                            topPadding: 4
                                            text: qsTr("<b>Год:</b> ") + qsTr(modelData.year)
                                        }
                                        Text {
                                            textFormat: Text.RichText
                                            font.pointSize: 10
                                            leftPadding: 8
                                            topPadding: 4
                                            width: 280
                                            wrapMode: Text.WordWrap
                                            maximumLineCount: 2
                                            text: qsTr("<b>Тип:</b> ") + qsTr(modelData.releaseType)
                                        }
                                        Text {
                                            font.pointSize: 10
                                            leftPadding: 8
                                            topPadding: 4
                                            width: 280
                                            wrapMode: Text.WordWrap
                                            maximumLineCount: 2
                                            text: qsTr("<b>Жанры:</b> ") + qsTr(modelData.genres)
                                        }
                                        Text {
                                            font.pointSize: 10
                                            leftPadding: 8
                                            topPadding: 4
                                            width: 280
                                            wrapMode: Text.WordWrap
                                            maximumLineCount: 2
                                            text: qsTr("<b>Озвучка:</b> ") + qsTr(modelData.voicers)
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    ColumnLayout {
        id: cardContainer
        visible: page.openedRelease ? true : false
        anchors.fill: parent
        spacing: 0
        Rectangle {
            color: "#D3D3D3"
            Layout.fillWidth: true
            Layout.fillHeight: true
            Column {
                Grid {
                    id: releaseInfo
                    columnSpacing: 3
                    columns: 3
                    bottomPadding: 4
                    leftPadding: 4
                    topPadding: 4
                    rightPadding: 4
                    Image {
                        id: cardPoster
                        source: page.openedRelease ? page.openedRelease.poster : '../Assets/Icons/donate.jpg'
                        fillMode: Image.PreserveAspectCrop
                        width: 280
                        height: 390
                        layer.enabled: true
                        layer.effect: OpacityMask {
                            maskSource: cardMask
                        }
                    }
                    Column {
                        width: page.width - cardButtons.width - cardPoster.width
                        enabled: !!page.openedRelease
                        Text {
                            textFormat: Text.RichText
                            color: "#a32727"
                            font.pointSize: 12
                            width: parent.width
                            leftPadding: 8
                            topPadding: 6
                            wrapMode: Text.WordWrap
                            maximumLineCount: 3
                            text: qsTr(page.openedRelease ? page.openedRelease.title : '')
                        }
                        Text {
                            textFormat: Text.RichText
                            font.pointSize: 10
                            leftPadding: 8
                            topPadding: 4
                            text: qsTr("<b>Статус:</b> ") + qsTr(page.openedRelease ? page.openedRelease.status : '')
                        }
                        Text {
                            font.pointSize: 10
                            leftPadding: 8
                            topPadding: 4
                            text: qsTr("<b>Год:</b> ") + qsTr(page.openedRelease ? page.openedRelease.year : '')
                        }
                        Text {
                            font.pointSize: 10
                            leftPadding: 8
                            topPadding: 4
                            text: qsTr("<b>Сезон:</b> ") + qsTr(page.openedRelease ? page.openedRelease.season : '')
                        }
                        Text {
                            textFormat: Text.RichText
                            font.pointSize: 10
                            leftPadding: 8
                            topPadding: 4
                            width: parent.width
                            wrapMode: Text.WordWrap
                            maximumLineCount: 2
                            text: qsTr("<b>Тип:</b> ") + qsTr(page.openedRelease ? page.openedRelease.releaseType : '')
                        }
                        Text {
                            font.pointSize: 10
                            leftPadding: 8
                            topPadding: 4
                            width: parent.width
                            wrapMode: Text.WordWrap
                            maximumLineCount: 2
                            text: qsTr("<b>Жанры:</b> ") + qsTr(page.openedRelease ? page.openedRelease.genres : '')
                        }
                        Text {
                            font.pointSize: 10
                            leftPadding: 8
                            topPadding: 4
                            width: parent.width
                            wrapMode: Text.WordWrap
                            maximumLineCount: 2
                            text: qsTr("<b>Озвучка:</b> ") + qsTr(page.openedRelease ? page.openedRelease.voicers : '')
                        }
                        Text {
                            textFormat: Text.RichText
                            font.pointSize: 10
                            leftPadding: 8
                            topPadding: 4
                            width: parent.width
                            wrapMode: Text.WordWrap
                            text: qsTr("<b>Описание:</b> ") + qsTr(page.openedRelease ? page.openedRelease.description : '')
                        }
                    }
                    Column {
                        id: cardButtons
                        width: 62
                        AppPanelButton {
                            iconSource: "../Assets/Icons/close.svg"
                            width: 60
                            onPressed: {
                                page.openedRelease = null;
                            }
                        }
                    }
                }
                Rectangle {
                    color: "transparent"
                    width: cardContainer.width
                    height: 60

                    Button {
                        anchors.verticalCenter: parent.verticalCenter
                        anchors.leftMargin: 10
                        anchors.left: parent.left
                        text: qsTr("Скачать")
                        onClicked: {
                        }
                    }

                    Text {
                        anchors.verticalCenter: parent.verticalCenter
                        anchors.left: parent.left
                        anchors.leftMargin: 100
                        font.pixelSize: 14
                        text: "Доступно "+ (page.openedRelease ? page.openedRelease.countTorrents : "0" ) + " торрентов"
                    }

                    Text {
                        anchors.verticalCenter: parent.verticalCenter
                        anchors.right: parent.right
                        anchors.rightMargin: 100
                        font.pixelSize: 14
                        text: "Доступно "+ (page.openedRelease ? page.openedRelease.countOnlineVideos : "0" ) + " серий онлайн"
                    }

                    Button {
                        text: qsTr("Смотреть")
                        anchors.verticalCenter: parent.verticalCenter
                        anchors.right: parent.right
                        anchors.rightMargin: 10
                        onClicked: {
                            watchRelease(page.openedRelease.id);
                        }
                    }

                    Row {
                        anchors.verticalCenter: parent.verticalCenter
                        anchors.right: parent.right
                        anchors.rightMargin: 10
                    }

                }
                WebView {
                    id: webView
                    visible: page.openedRelease ? true : false
                    width: cardContainer.width
                    height: cardContainer.height - releaseInfo.height - 60
                    url: page.openedRelease ? "https://vk.com/widget_comments.php?app=5315207&width=100%&_ver=1&limit=8&norealtime=0&url=https://www.anilibria.tv/release/" + page.openedRelease.code + ".html" : "_blank";
                    onLoadingChanged: {
                        //if (loadRequest.errorString) console.error(loadRequest.errorString);
                    }
                }
            }
        }

    }

    function selectItem(item) {
        if (page.selectMode) {
            if (page.openedRelease) page.openedRelease = null;
            item.selected = !item.selected;
            selectedReleases.push(item);
        } else {
            page.openedRelease = item;
        }
    }
}
