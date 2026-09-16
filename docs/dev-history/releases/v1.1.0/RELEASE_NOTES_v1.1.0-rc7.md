# Ferry v1.1.0 RC7

RC7は、RC6のBreadcrumb横スクロールバー表示と固定高さを維持しながら、Toolbarの縦方向バランスを整えるUI仕上げ候補です。

## Changes

- Breadcrumb専用horizontal scrollbarを約8pxへ縮小。
- Navigation / view / settings等のicon buttonを30×30の正方形へ統一。
- Search mode、Search box、Location Boxを30px高へ揃えた。
- Breadcrumb / Location Box共通hostはScrollbar 8px分を含めた固定高さ。
- Rubber-band selection、Selection Anchor、autoscroll設定、Sidebar auto-fit、Ctrl+L挙動はRC6から変更なし。

## Validation

Windows実機では、長いBreadcrumbでScrollbarが実際に8px程度へ細く見えること、path文字列が欠けないこと、Toolbar buttonが正方形で縦に伸びないことを重点確認してください。
