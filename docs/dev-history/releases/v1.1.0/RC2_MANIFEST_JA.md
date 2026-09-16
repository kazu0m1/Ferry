# Ferry v1.1.0 RC2 Manifest

## 基準線

- Application: v1.1.0 RC1
- RC1 Windows validation: PASS
- Selection engine baseline: Prototype 27

## RC2で追加したapplication変更

1. Location Boxのfile-view dismiss
   - Location Box表示中にList/Gridをマウス操作するとBreadcrumbへ復帰。
   - この経路ではCtrl+L直前のfocusを復元せず、続くmouse gestureにfocusを委ねる。
   - Esc / Ctrl+L再押下のRC1経路は維持。

2. Selection Anchor visualの弱化
   - 1 px。
   - `#707070`相当のdark gray。
   - DashArray: `2,3`。
   - selection/anchorロジック自体は変更なし。

3. Rubber-band autoscroll速度設定
   - `AppSettings.RubberBandAutoScrollSpeed`を追加。
   - 設定範囲: 30–150。
   - default: 100。
   - 旧settings.jsonでproperty欠落時は100へnormalize。
   - SettingsのSave / Reset / Import / Export対象。
   - validated acceleration curveは維持し、最大速度だけを設定値で制限。
   - 150を実効可能にするため25 ms timerのper-tick guardを最大4 stepへ拡張。default 100では従来の速度領域を維持。

## バージョン

- Window title: `Ferry - RC 2`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc2`
- App manifest: `1.1.0.0`
- Portable package label: `1.1.0-rc2`

## 変更していない主要領域

- Prototype 27 / RC1 rubber-band selection semantics
- Shift anchor semantics
- D&D routing
- List/Grid selection synchronization
- double-click item-open guard
- stationary whitespace keyboard-focus fix
- sorting / folder-first
- ZIP subsystem

## 実機確認

`V1.1.0_RC2_TEST_JA.md`を使用する。
