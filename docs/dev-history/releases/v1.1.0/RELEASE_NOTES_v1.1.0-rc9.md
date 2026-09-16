# Ferry v1.1.0 RC9

RC9は、RC8で常時表示化したBreadcrumb horizontal scrollbarの**操作性と外観**を仕上げる局所UI候補です。

## 変更点

- Breadcrumb専用horizontal scrollbarを8pxから**10px**へ拡大。
- scrollbar thumbを10px高にし、pointerで掴みやすくした。
- thumbの四隅を**2px radius**で軽く丸めた。
- 左右end buttonも10px高とし、四隅を**2px radius**で軽く丸めた。
- RC8のend button常時表示、通常 `#B8B8B8` / hover `#A2A2A2` / pressed `#8E8E8E`、arrow常時表示は維持。
- Breadcrumb / Location Box固定高さは30px + 10px scrollbar分へ追随。
- Toolbar 30×30 square button、Location Box、rubber-band / Selection Anchor / autoscroll / Sidebar auto-fit / List↔Grid syncには変更なし。

## バージョン

- Window title: `Ferry - RC 9`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc9`

Windows実機では `V1.1.0_RC9_TEST_JA.md` の局所確認を行ってください。
