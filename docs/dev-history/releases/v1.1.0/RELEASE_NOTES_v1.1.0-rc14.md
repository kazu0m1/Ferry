# Ferry v1.1.0 RC14

RC14は、RC13で実機修正を確認したkeyboard-navigation fixを**挙動変更なしでrelease candidate化**した版です。

## RC13で確定した修正

true backgroundを通常クリックしてselectionが0件になり、ListView/ListBox本体がkeyboard focusを持つ状態で最初の`Up` / `Down`を押した場合、WPF標準navigationへそのまま渡さず、Ferryが最後のkeyboard-origin itemから隣接itemを決定します。

- 最初の矢印だけFerryが補正。
- target itemへsingle selection / scroll / keyboard focus / selection anchorを同期。
- item focusへ復帰した後は通常のWPF keyboard navigationへ戻る。
- RC13実機logでは、先頭→空白→Downでindex 0をoriginとしてindex 1へ移動し、その後はindex 2, 3, 4...と通常WPF navigationへ復帰。
- 末尾側でも空白→Upでindex 78→77をFerryが補正し、その後76, 75...と通常navigationへ復帰。

## RC14での変更

- `Logger.Configure(true)`を撤去し、通常どおり`settings.DebugLogging`に従う。
- `[RC13_NAV]`診断ログ用コードを削除。
- keyboard-navigation fix本体はRC13から変更なし。
- Window title / package metadataをRC14へ更新。

## 変更しないもの

- Rubber-band state machine / modifier semantics
- Selection Anchor visual / Shift anchor semantics
- List↔Grid selection sync
- D&D
- autoscroll 30–300
- Breadcrumb / Location Box / toolbar
- Sidebar auto-fit

## Version

- Window title: `Ferry - RC 14`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc14`
- Portable package label: `1.1.0-rc14`

Windows実機では`V1.1.0_RC14_TEST_JA.md`を使用してください。
