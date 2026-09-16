# Ferry v1.1.0 RC4 Manifest

## 基準線

- Application: v1.1.0 RC3
- RC3 Windows validation: PASS
- Selection engine baseline: Prototype 27

## RC4で追加したapplication変更

1. Toolbar / path host高さ固定
   - Breadcrumb表示時とLocation Box表示時でtoolbarの高さが変わる現象を修正。
   - `locationBox`はBreadcrumb表示中に`Collapsed`ではなく`Hidden`とし、layout上のdesired heightを保持。
   - `pathHost.Loaded`でLocation Boxの`DesiredSize.Height`を取得し、`Height / MinHeight / MaxHeight`を同一値へ固定。
   - Breadcrumb側にhorizontal scrollbarが必要な場合も、toolbar自体を縦方向へ拡張しない。

2. RC3機能
   - Location Box dismissal、Selection Anchor、autoscroll 30〜300、Sidebar auto-fitを変更していない。
   - Rubber-band / Shift anchor / D&D / List↔Grid同期等の選択ロジックを変更していない。

## バージョン

- Window title: `Ferry - RC 4`
- AssemblyVersion/FileVersion: `1.1.0.0`
- InformationalVersion: `1.1.0-rc4`
- Portable package label: `1.1.0-rc4`

## 実機確認

`V1.1.0_RC4_TEST_JA.md`を使用する。
