# Ferry v1.1.0 RC15 — Manifest

## 目的

RC14で確認された2点をExplorer実機挙動へ合わせる最終keyboard-navigation候補。

1. 無修飾Arrowで移動した後もFerryの破線Selection Anchorが古いmouse-click位置に残る。
2. true backgroundで0件選択後、`Shift+Down`をWPFへ渡すと末尾へjumpする。

## Explorer観察に基づく仕様

- Aをclickしてから無修飾ArrowでFまで移動した場合、その後のShift+ArrowはFを範囲起点にする。
- Aをclick → 完全空白で選択解除 → `Shift+Down` はA/Bを選択する。

## 実装方針

- 無修飾Arrow: WPF標準navigationをそのまま使う。処理後に実際のfocused itemをFerry logical anchorへ同期。
- 0件選択 + Selector focus + Shift+Arrow: Ferryが保持しているkeyboard originをanchorにし、隣接itemまでのrangeを明示的に作る。
- 最初の修復後はitem focus/currentとWPF anchorを同期し、通常WPF navigationへ戻す。
- RC13の無修飾empty-selection repairは維持。
- 強制diagnostic loggingは入れない。

## Version metadata

- Window title: `Ferry - RC 15`
- InformationalVersion: `1.1.0-rc15`
- Portable package label: `1.1.0-rc15`

## Runtime gate

`V1.1.0_RC15_TEST_JA.md` の最重要1～4をPASSするまでfinalへ昇格しない。
