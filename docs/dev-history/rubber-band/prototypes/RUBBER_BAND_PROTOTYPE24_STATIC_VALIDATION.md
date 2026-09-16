# Ferry v1.0.2 Rubber-band Integration Prototype 24 — 静的検証

## 基準線

Prototype 23。

## 変更ファイル

- `Source/Ferry/MainWindow.cs`
- `Source/Ferry/AssemblyInfo.cs`

上記以外のアプリケーションソースはPrototype 23と同一。

## 修正内容

Prototype 23では `Selector.MouseDoubleClick` がList/Grid全体に登録され、イベント発生位置をitem containerへ限定せず `OpenSelected(state)` を実行していた。このためScrollBarやtrue background上のダブルクリックもSelectorへbubbleし、既に選択されているファイル/フォルダーを開く可能性があった。

Prototype 24では `HandleViewMouseDoubleClick()` を追加し、左ダブルクリックについて以下を確認した場合だけ `OpenSelected(state)` を呼ぶ。

1. inline rename editorではない。
2. ScrollBar / Thumb / Track / RepeatButton / GridViewColumnHeader等のview chromeではない。
3. `OriginalSource` のvisual ancestorに実際の `ListViewItem` または `ListBoxItem` が存在する。
4. そのcontainerのDataContextが `FileItem` である。

これによりScrollbarおよびtrue backgroundのダブルクリックはopen actionから除外され、item上の既存double-click挙動は維持される。

## 影響範囲

- rubber-band gesture routing: 変更なし
- Ctrl / Shift / Ctrl+Shift selection: 変更なし
- Shift anchor: 変更なし
- D&D: 変更なし
- autoscroll: 変更なし
- List/Grid selection sync: 変更なし
- F5/refresh: 変更なし

## バージョン表示

- Window title: `Ferry - prototype 24`
- Informational version: `1.0.2-rubberband-prototype24`

Windows実機ビルド/動作確認は別途必要。
