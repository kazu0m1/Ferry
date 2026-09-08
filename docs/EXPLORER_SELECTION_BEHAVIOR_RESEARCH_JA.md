# Explorer選択動作 調査記録（v1.0では実装保留）

> **Status:** Design research only / not part of Ferry public v1.0 requirements.
>
> RC1〜RC10でExplorerの選択動作を調査した記録です。公開v1.0では安定性を優先し、Ferry v1.0.7.2のnative WPF Extended selectionへロールバックしました。ラバーバンド選択は実装しません。将来版で再検討する場合の参考資料として保持します。

対象: Ferry v1.0 public release candidate
仕様基準: 2026-09-08 に Windows Explorer 実機で直接観察した挙動
位置づけ: `Ferry_SPEC_v1.0.md` F05 Selection / Interaction の詳細仕様

## 1. 状態モデル

Ferry は以下を別々の状態として扱う。

- **Hovered**: ポインターが row/tile 上にある。実選択ではない。
- **Pressed / Pending**: mouse-down 済みだが、mouse-up / drag のどちらへ遷移するか未確定。
- **Selected**: 実際の Copy / Delete / Enter / D&D 等の操作対象。
- **Focused / Current**: keyboard navigation の現在位置。Selected とは独立し、0件選択時にも1 itemだけ保持できる。
- **Inactive Selected**: Selected だが file view が keyboard focus を失っている状態。
- **Selection Anchor**: Shift range selection の起点。
- **Rubber-band Anchor**: rubber-band を開始した row/tile。rubber-band後に選択を解除しても Focused/current の復帰位置として維持する。

### 表示の基準

| 状態 | 表示 |
|---|---|
| Normal | 選択色なし |
| Hovered | 薄い青 |
| Pressed/Pending + Focused | 薄い青 + focus実線 |
| Selected + active file view | 濃い青 |
| Selected + Focused | 濃い青 + focus実線 |
| Inactive Selected | 灰色 |
| Focused only (Selected=false) | 選択色なし + focus実線 |

Selected / Focused / Hover / Pending を同一のWPF状態として扱ってはならない。

## 2. 未選択 row/tile の余白

1. pointer hover → Hovered（薄い青）。
2. mouse-down → Focused/current はその row/tile へ移動し、Pressed/Pending（薄い青 + focus実線）。既存の確定Selectionはこの時点では維持する。
3. pointerが動かず mouse-up → その row/tile だけを Selected に確定。以前の単一/複数Selectionは解除する。
4. system drag threshold を超える → Pending item を Selected に確定して Rubber-band Anchor とし、rubber-bandを開始する。
5. double-click（pointerが移動しない） → owning folder/file を開く。

## 3. 既にSelectedの row/tile

- row/tile の icon/name だけでなく余白からも file D&D を開始できる。
- 複数Selectedの1 itemからdragした場合、Selected set全体をD&D対象とする。
- dragせずsingle-clickを完了した場合、Ctrl/Shiftなしなら mouse-up 時にその1 itemだけへSelectionをcollapseする。
- Ctrlを押したclickでは mouse-up 時に対象itemのSelectedをtoggleする。

## 4. True background

row/tile に属さない完全空白では:

- mouse-downだけではSelectionを変更しない。
- stationary mouse-up → SelectedItemsを0件にする。
- Focused/current itemは保持する。
- 表示は「選択色なし + focus実線」のみ。
- ↓キーを1回押すと、Focused/currentの次のitemをSelected + Focusedにする。
- system drag thresholdを超える → rubber-bandを開始する。
- double-click → itemを開かない。selectionは解除状態になる。

## 5. Rubber-band

- selection geometry は icon/name ではなく **List row / Grid tile全体**。
- rectangleとrow/tile containerが交差すればそのitemをSelected対象とする。
- unselected row/tile whitespaceから開始した場合、そのrow/tileはmovement commit時にSelected + Focusedとなり Rubber-band Anchorになる。
- rubber-band終了後、Selected setと表示は一致しなければならない。
- 完全空白clickでrubber-band selectionを解除した場合、Focused/currentはRubber-band Anchorに戻る。
- Ctrl + rubber-band は既存Selectionを保持して追加する。

## 6. Ctrl / Shift

### Ctrl multiple selection

- 複数itemをSelectedできる。
- Focused/currentは最後にfocusされたitem。
- 完全空白click後はSelectedItems=0、Focused/currentは最後のfocused itemを保持する。

### Shift range selection

- Selection Anchorからクリックしたendpointまで連続Selected。
- Focused/currentはrangeの最後（endpoint）。
- Selection AnchorとFocused/currentを同一視しない。

## 7. Enter

### 複数files

- 全Selected filesをWindows file associationで開く。

### 複数folders

Explorer観察:
- Focused folderは既存windowで開く。
- その他Selected foldersは新規windowで開く。

Ferry v1.0の適応仕様:
- Focused folderはcurrent Ferry tabで開く。
- その他Selected foldersはadditional Ferry tabsで開く。

## 8. File view focusを失ったとき

- Selected itemは灰色のinactive selection表示。
- Focused/current outlineは残る。
- pointerがSelected row/tile上へ戻ると、そのitemはactive selection相当の濃い青表示にできる。
- Hoverはreal Selectionを変更しない。

## 9. Refresh / actively changing files

Explorer観察:
- F5やsort後も同一itemのSelectionは維持される。
- `.crdownload`等の書き込み中itemもsingle-click / Ctrl+Click / D&D / F2が可能。
- download完了rename後、focusは引き継がれ、selection backgroundは解除される挙動を確認した。
- 外部からsort上方にitemが挿入された場合、Explorerでは位置依存に見えるSelection移動も観察された。

Ferry v1.0の適応仕様:
- 同一pathが存在する限り既存 `FileItem` instanceを更新し、metadata変更だけでUI itemをreplaceしない。
- Selection / Focusをpath identityで安定維持する。
- 外部挿入によるrow-position selection移動は模倣しない。
- Windows rename eventでFocused/current / anchor pathをold→newへ転送できる場合は転送する。
- external renameでSelectionそのものを引き継ぐことは必須としない。

## 10. 実装原則

1. WPF keyboard focusをSelection状態の代用品にしない。
2. unselected item containerへfocusを戻すことでFocused/currentを表現しない。WPFが暗黙的にselectするため。
3. Focused/currentはpathで保持し、focus実線をFerry側で描画する。
4. real operation targetは常に`SelectedItems`。
5. hover/pending visualは`SelectedItems`へ追加してはならない。
6. pointer gestureはmouse-down時に即断せず、mouse-up / double-click / drag thresholdによってcommitする。
7. live refreshはsame-path item identityを維持する。
