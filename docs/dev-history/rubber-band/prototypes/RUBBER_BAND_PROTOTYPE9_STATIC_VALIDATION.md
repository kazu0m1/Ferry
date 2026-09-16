# Ferry v1.0.2 Rubber Band Integration Prototype 9 — Static Validation

## Baseline
Prototype 6 を直接ベースに作成。Prototype 7/8 の実装は取り込んでいない。

## Intended application-logic delta
`Source/Ferry/MainWindow.cs` の変更意図は次の2点のみ。

1. ウィンドウタイトルを `Ferry - prototype 9` に変更。
2. Prototype 6 の `NotifyListItemClicked` 反射呼び出しを廃止し、stationary row-whitespace click では:
   - 従来どおり単一選択を確定;
   - WPF `ListBox` の非公開 `AnchorItem` をクリック項目へ直接更新;
   - 利用可能なら `LastActionItem` も対応containerへ更新。

Gesture routing、true-background clear、rubber-band集合演算、D&D経路には変更を加えていない。

## Rationale
Prototype 6 は実機で最も良い応答を示したが、高速反復時にまれなShift-anchor missが残った。Prototype 6のprivate `NotifyListItemClicked` 呼び出しは、anchor更新以外にもmouse capture/focus/modifier-dependent処理を含むため、Prototype 9では必要なanchor状態だけを直接更新する。

## Static checks
- MainWindow delimiter balance: PASS
- Prototype title marker: PASS
- Assembly informational version marker: PASS
- `NotifyListItemClicked` reference removed from Prototype 9 application source: PASS
- `AnchorItem` / `LastActionItem` explicit update markers present: PASS
- Prototype 6 vs Prototype 9 source diff is localized to MainWindow anchor helper/title and AssemblyInfo metadata: PASS

## Windows-only gate
この環境では Ferry の Windows/.NET Framework 4.8 WPF 実行ビルドは行えない。`Build.cmd` と実機試験を最終ゲートとする。
