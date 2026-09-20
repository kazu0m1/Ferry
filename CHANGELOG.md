# Ferry Changelog

## [1.1.6] - 2026-09-20

### Added
- Windowsの `This PC` Shell namespaceから、ドライブ文字を持たないMTP / Portable Deviceを認識するようにした。
- 接続中のAndroidスマートフォン等をSidebarの **Portable Devices** セクションへ表示するようにした。
- Portable Deviceをクリックすると、その端末をWindows Explorerで開く簡易連携を追加した。

### Scope
- Ferry内部でMTPストレージの列挙・コピー・削除等は実装しない。Portable Deviceのファイル操作はWindows Explorerへ委譲する。
- 接続・切断の更新には既存の `WM_DEVICECHANGE / DBT_DEVNODES_CHANGED` によるSidebar再構築を再利用する。

### Validated
- Windows GitHub Actions `Build.cmd`: PASS。
- Pixel 7aをUSB接続しファイル転送モードへ切り替えた際、Ferry Sidebarで端末認識: PASS。
- Sidebarの端末項目クリックからWindows ExplorerでPixel 7aを開く動作: PASS。

## [1.1.5] - 2026-09-20

### Added
- 複数タブをマウスD&Dで左右へ並べ替えられるようにした。ドロップ位置は対象タブの左右半分で判定し、移動後もドラッグしたタブを選択状態に保つ。
- Sidebarの通常フォルダー（Home / 標準ユーザーフォルダー / Drives）に右クリックメニューを追加し、**Open**、Open in New Tab、Open in New Ferry Window、Open Terminal Here、Open in Explorer、Properties、Show more optionsを利用可能にした。
- Pinned folderにも同じSidebarフォルダーメニューを適用し、従来の **Unpin** を維持した。

### Preserved
- タブ並べ替えは専用の `Ferry.TabItem` D&D formatを使用し、既存のファイルD&D / Ferry→Ferry D&D / Pinned folder並べ替えとは分離。
- タブCloseボタンからは並べ替えドラッグを開始しない。
- v1.1.4 responsive Shell transfer、v1.1.3 removable-drive lifecycle、v1.1.2 external D&D Move completion、v1.1.1 Paste-result feedback、v1.1.0 selection behaviorを変更しない。

### Validated
- Windows GitHub Actions `Build.cmd`: PASS。
- 3タブ程度での左右ドラッグ並べ替え: PASS。
- タブの `×` Close操作が従来どおり機能: PASS。
- Sidebar folder右クリック → **Open**: PASS。
- Pinned folder右クリックで **Unpin** が維持されること: PASS。

## [1.1.4] - 2026-09-20

### Fixed
- 長時間のWindows Shell Copy/Move中にFerryのWPF UI threadが塞がり、最小化したwindowを転送完了まで復元できない問題を修正。
- Copy/Moveの `SHFileOperation` を専用STA workerで実行し、転送中もFerryのDispatcherがwindow描画・最小化/復元・tab操作・folder navigationを処理できるようにした。
- Ferry→Ferryの長時間D&Dで、受信側Drop handlerがtransfer完了まで戻らず送信側が `DragDrop.DoDragDrop(...)` に拘束される問題を修正。受信側はDropを速やかに受理し、実transferはtarget側STA workerで継続する。
- Ferry-owned Copy/Move実行中にFerryを閉じないようclose guardを追加し、worker transferがapplication終了で失われないようにした。

### Validated
- 大容量C:→D: / D:→C: Cut→Paste Moveの正常完了: PASS。
- 大容量Move中の最小化/復元、tab追加/切替、folder navigation: PASS。
- 大容量Copy中の最小化/復元、tab切替、正常完了: PASS。
- active transfer中のclose guard: PASS。
- Ferry→Ferry D&D中、送信側・受信側とも最小化/復元: PASS。
- Ferry→Ferry D&D中、送信側で複数tab open / folder navigation: PASS。
- 同一drive内Ferry→Ferry D&D Moveでsource消失・destinationに1件のみ存在: PASS。
- Windows GitHub Actions `Build.cmd`: PASS。

### Preserved
- Windows ShellはCopy/Moveの実処理・progress/conflict/cancel UIのauthorityのまま。独自copy engineは導入しない。
- Paste側は既存のoperation-completion contractとv1.1.1 Paste-result feedbackを維持。
- Delete処理は今回のbugfixでは変更しない。
- v1.1.3 removable-drive lifecycle、v1.1.2 external D&D Move completion、v1.1.0 selection/rubber-band behaviorを維持。

## [1.1.3] - 2026-09-20

### Fixed
- Ferry起動後に接続したリムーバブルドライブがSidebarの **Drives** に反映されない問題を修正。
- ドライブ接続・切断をWindowsの `WM_DEVICECHANGE` で受け、Sidebarのドライブ一覧を自動更新するようにした。
- Ferryでリムーバブルドライブを開いているとWindowsの **Safely Remove Hardware** が「このボリュームは現在使用中です」で失敗する問題を修正。
- 対象ドライブのsafe-eject queryを受けたら、Ferryの通知ハンドル / `FileSystemWatcher` / 関連バックグラウンド処理を解放し、そのドライブを開いている全タブをHome等のローカルfallbackへ退避してからWindowsへ制御を返す。

### Validated
- Ferry起動後のリムーバブルドライブ接続でSidebarへ自動表示: PASS。
- 取り外し後のSidebar更新、再接続・再表示・再Open: PASS。
- リムーバブルドライブを1タブで開いた状態からのsafe eject: PASS。
- 同じリムーバブルドライブを2タブで開いた状態からのsafe eject: PASS。両タブがHomeへ退避し、Windows側の取り外しも成功。

### Preserved
- v1.1.2 external D&D Move completion、v1.1.1 Paste result feedback、v1.1.0 Selection Engine / rubber-band / Selection Anchor / autoscrollには変更なし。

## [1.1.2] - 2026-09-16

### Fixed
- Ferry → Windows Explorer の同一ドライブ通常D&Dで、Explorer側へitemが作成されてもFerry側sourceが残り、結果がCopyのようになる場合を修正。
- External D&D終了時にWPFのfinal drop effectとWindows Shellの`Performed DropEffect`を確認し、両方がMoveを示すunoptimized MOVEの場合だけsource cleanupを行う。
- optimized MOVEでsourceが既に消えている場合は追加削除しない。

### Validated
- v1.1.2 prototype 1はWindows実機で、同一ドライブMove、`Ctrl+D&D` Copy、Cancel、folder、複数item、Ferry→Ferry、Explorer→Ferry、Paste-result feedback、Selection最小回帰をPASS。
- 別volumeへのD&Dはテスト環境に第二ドライブがないため未実施。prototype checklist上の任意項目として未検証を明記する。

### Preserved
- Ferry→Ferry internal D&D、Explorer→Ferry D&D、v1.1.1 Paste result feedback、v1.1.0 Selection Engine / rubber-band / Selection Anchor / autoscrollには変更なし。

## [1.1.1] - 2026-09-16

### Added
- Explorer-style Paste result feedback: top-level items created or updated by the current Copy/Cut → Paste operation are selected in the destination view so users can immediately see what was pasted.
- Paste result selection is mirrored between List and Grid and retained after the final incremental refresh.
- A bounded per-tab Paste feedback session tracks only the current operation; a later Paste replaces the previous result selection instead of accumulating stale results.

### Validated
- v1.1.1 prototype 1 passed Windows real-machine testing for Copy/Paste, overwrite/merge cases, Cut/Paste, List/Grid synchronization, selection clearing, selection/D&D regression, and consecutive Paste operations.
- Same-folder Paste follows Windows standard conflict handling; in the tested environment Windows presented its conflict dialog and the operation was skipped/cancelled rather than generating a Ferry-owned duplicate name.

### Preserved
- Windows `SHFileOperation` remains the authority for Copy/Move/conflict handling; Ferry does not introduce a custom copy engine.
- v1.1.0 rubber-band, Selection Anchor, keyboard navigation, true-background, D&D and incremental-refresh behavior are otherwise unchanged.

## [1.1.0] - 2026-09-16

### Released
- RC20 passed final Windows validation and is promoted to Ferry v1.1.0 with no further application-behavior changes.
- Final v1.1.0 freezes the validated List/Grid selection, keyboard navigation, true-background geometry, rubber-band selection, D&D coexistence, Breadcrumb/Location Box, toolbar, and Sidebar behavior.
- Release metadata changed from RC20 to final (`Ferry`, informational version `1.1.0`, portable package version `1.1.0`).
- README screenshot gallery updated with the approved v1.1.0 release-candidate screenshots.


## [1.1.0-rc20] - 2026-09-16

### Fixed
- List左gutterのGridView headerが独自の`SystemColors.ControlBrush`で塗られ、他のdata-column headerと色がずれる視覚的不整合を修正。
- gutter headerの明示背景色を廃止し、通常のGridViewColumnHeaderと同じWindows/WPF theme backgroundを使用。
- RC19でPASSしたselection fill clipping、true-background gutter/tail、rubber-band、D&D、List/Grid keyboard navigationには変更なし。

## [1.1.0-rc19] - 2026-09-16

### Fixed
- Details/List selection fill is now visually clipped to the same data-column span used by the RC18 Selection Anchor and interaction geometry.
- The left true-background gutter and the area right of the final visible column remain visually unselected while preserving the native WPF/theme selection appearance inside the data span.
- No keyboard-navigation, Grid navigation, rubber-band selection, D&D, or column-geometry logic changed from RC18.

## [1.1.0-rc18] - 2026-09-16

### Changed
- List表示の左端に10pxのExplorer風true-background gutterを追加し、最左データカラムとの間にrubber-band開始用の余白を確保。
- ListのSelection Anchor破線をListView全幅ではなく、左gutterを除いた「最左データカラム〜最右データカラム」の範囲に限定。
- 最右データカラムより右側を、行の高さ内であってもFerry上のtrue backgroundとして扱うようhit testingを統一。
- Listのrubber-band intersection geometryも同じdata-column spanへ統一。

### Fixed
- 左gutter／右側tailでのクリック、rubber-band開始、右クリック・中クリック、double-click、drop target判定が行itemとして誤認されないよう統一。
- RC17でPASSしたGrid 4方向keyboard navigation、およびRC16までのList keyboard navigationは変更しない。

## [1.1.0-rc17] - 2026-09-16

### Fixed
- Grid表示の通常`Left`/`Right`後にも、Listと同様にSelection Anchorを実際の選択/focus itemへ同期するよう修正。
- Gridでtrue-background clear後に`Left`/`Right`を押してもkeyboard navigationが復帰しない経路を修正。
- Gridでtrue-background clear後の`Up`/`Down`をListの`index ± 1`として扱っていた暫定処理を廃止し、`VirtualizingWrapPanel`の実際の列数から視覚方向を計算する。
- List表示のRC16 PASS済みkeyboard navigationには変更を加えない。

## [1.1.0-rc16] - 2026-09-15

### Fixed
- RC15で確認された、無修飾`Up`/`Down`後に破線Selection Anchorが実際の選択/focusより1 item遅れて追随するoff-by-one表示・論理同期を修正。
- PreviewKeyDownから`DispatcherPriority.Input`で同期する方式を廃止し、WPF ListView/ListBoxがnative Arrow navigationを完了した後のbubble-phase `KeyDown`で現在itemを同期する。
- RC15でPASSしたtrue-background clear後の無修飾Arrow、および`Shift+Arrow`のA/B範囲回復は維持。
- Rubber-band / D&D / autoscroll / Breadcrumb / Sidebar等は変更しない。

## [1.1.0-rc15] - 2026-09-15

### Fixed
- Explorer実機観察に合わせ、通常の`Up`/`Down`で到達したitemを次のkeyboard Shift範囲の起点としてFerryのSelection Anchorにも同期。
- true-backgroundで0件選択にした直後の`Shift+Up` / `Shift+Down`をFerryが明示処理し、保持していたkeyboard originから範囲選択を再開するよう修正。
- `Aを選択 → 完全空白で選択解除 → Shift+Down` はExplorer同様に`A+B`を選択し、viewport端へjumpしない。
- RC13の無修飾`Up`/`Down`救済経路は維持。診断用の強制ログは追加しない。

## [1.1.0-rc14] - 2026-09-15

### Changed
- RC13で実機PASSした「true backgroundで0件選択 + Selector本体focus」時の最初のUp/Down補正をそのまま維持。
- RC12/RC13の強制keyboard-navigation診断ログを撤去し、通常のDebug Logging設定へ復帰。
- application behaviorはRC13から変更せず、release-candidate hygieneと回帰確認に限定。

## [1.1.0-rc13] - 2026-09-15

### Fixed
- true-backgroundで0件選択にした後、Selector本体にkeyboard focusがある状態で`Up`/`Down`を押すとviewport端へjumpする経路を局所修正。
- 最後にkeyboard focusを持っていたitemをFerry側で追跡し、選択0件 + Selector focus時の最初の`Up`/`Down`だけをFerryが決定的に処理する。
- その後はitem focus / selection / WPF private anchor/currentを同期し、通常のWPF keyboard navigationへ戻す。
- Rubber-band / Selection Anchor / List↔Grid sync / autoscroll / Breadcrumb等の既存挙動は変更しない。

## [1.1.0-rc12] - 2026-09-15

### Diagnostic
- true-background clear後に最初の`Down`で末尾へjumpする経路を診断するため、keyboard focus / WPF AnchorItem / LastActionItem / CollectionView CurrentItemをログ出力する診断版。
- RC11の動作は維持し、選択ロジックそのものには追加修正を入れていない。
- RC12では`Ferry.log`を必ず生成する（diagnostic build限定）。

## [1.1.0-rc11] - 2026-09-15

### Fixed
- Fixed a remaining keyboard-navigation jump path after a plain true-background click clears selection.
- A stationary true-background click now restores the previously focused realized item as keyboard current while keeping selection empty.
- Actual rubber-band drags keep the validated v1.1.0 behavior unchanged.

## [1.1.0] - 2026-09-15

### Added
- Explorer-style rubber-band selection for List and Grid, including validated Ctrl / Shift / Ctrl+Shift behavior and edge autoscroll.
- Selection Anchor visualization and configurable rubber-band autoscroll speed (30–300, default 100).
- Sidebar divider double-click auto-fit.

### Changed
- `Ctrl+L` Location Box can return to Breadcrumb via `Esc`, a second `Ctrl+L`, or file-view mouse interaction.
- Toolbar icon buttons are standardized to 30×30 and the path row no longer changes height between Breadcrumb and Location Box.
- Breadcrumb overflow uses a dedicated 10px horizontal scrollbar with always-visible line buttons. Final chrome is rectangular for consistency with Ferry's square controls.
- List/Grid selection synchronization and keyboard-current handling were hardened for large folders.

### Fixed
- Prevented scrollbar double-clicks from opening the currently selected item.
- Prevented row-whitespace keyboard navigation from jumping to the top/bottom in large virtualized folders.

## [1.1.0-rc10] - 2026-09-15

### Changed
- Breadcrumb専用horizontal scrollbarのthumbと左右end buttonのcorner radiusを2pxから**5px**へ拡大し、より楕円的で視認しやすい外観へ調整。
- 10px高さ、常時表示end button、gray chrome、固定path-host高さなどRC9で確定したBreadcrumb scrollbar仕様は維持。
- Toolbar 30×30 square button、Location Box、rubber-band / selection / autoscroll等の挙動は変更なし。

## [1.1.0-rc9] - 2026-09-15

### Changed
- Breadcrumb専用horizontal scrollbarの高さを8pxから10pxへ拡大し、thumbを掴みやすくした。
- Breadcrumb scrollbarのthumbと左右end buttonに2pxのcorner radiusを追加し、軽い角丸へ変更。
- RC8で導入したend button常時表示とnormal / hover / pressedの濃度差は維持。
- Toolbar 30×30 square button、Location Box、rubber-band / selection / autoscroll等の挙動は変更なし。

## [1.1.0-rc8] - 2026-09-15

### Changed
- Breadcrumbの8px horizontal scrollbar両端にある左右移動buttonを、mouse hover前から常時見える専用chromeへ変更。
- End buttonはscrollbar本体より一段濃いgrayを通常色とし、hover / pressedで段階的に濃くなる。
- 8px scrollbar、Toolbar 30×30 square button、固定path-host高さなどRC7のlayoutは維持。

## [1.1.0-rc7] - 2026-09-15

### Changed
- Breadcrumb用horizontal scrollbarを約8pxへ縮小し、List/Grid側の標準scrollbarには影響しないよう局所化。
- Toolbarのicon button（Back / Forward / Up / Home / New tab / List / Grid / Settings / Search clear）を30×30の正方形へ統一。
- Search mode / Search box / Location Boxも30px高へ揃え、RC6のScrollbar分でbuttonが縦に伸びる見た目を解消。
- Breadcrumb / Location Box共通hostは30px + 8px scrollbar分の固定高さとし、長いpath表示と高さ固定を両立。

## [1.1.0-rc6] - 2026-09-15

### Fixed
- Restored the Breadcrumb horizontal scrollbar while keeping the path row at a constant height.
- Reserved one horizontal-scrollbar row in the fixed path-host height so long Breadcrumbs remain fully visible vertically.

## [1.1.0-rc5] - 2026-09-15

### Fixed
- Fixed Breadcrumb content being clipped/hidden when the path host was fixed to the Location Box height.
- Breadcrumb horizontal overflow no longer consumes vertical toolbar space; long paths keep the current-location side visible.

## v1.1.0 RC4 — 2026-09-15

Final toolbar-layout polish candidate following the Windows PASS of v1.1.0 RC3.

- Fixes the toolbar/path-row height to the Location Box height so toggling between Breadcrumb and Ctrl+L does not resize the menu/toolbar vertically.
- Keeps the Location Box hidden (rather than collapsed) while Breadcrumb is shown so its desired height remains available to layout.
- Constrains the path host to that measured height; Breadcrumb horizontal scrolling must fit inside the same row instead of growing the toolbar.
- Promotes version metadata to `1.1.0-rc4`; all RC3 interaction behavior is otherwise unchanged.


## v1.1.0 RC3 — 2026-09-15

Focused interaction-polish candidate following the Windows PASS of v1.1.0 RC2.

- Keeps RC2 Location Box dismissal behavior unchanged.
- Restores the Prototype 27 Selection Anchor dash rhythm (`1,1`) while retaining RC2's softer dark-gray `#707070` stroke.
- Expands **Rubber-band autoscroll speed** to **30–300**, default **100**.
- Makes the speed setting scale the full acceleration curve, so 50 / 100 / 200 / 300 are perceptibly different at the same pointer distance; 100 preserves the validated previous curve.
- Adds Explorer-like Sidebar auto-fit: double-clicking the Sidebar/file-view splitter fits the Sidebar to its displayed labels, within the existing 50–480 width limits, and persists the resulting width.
- Promotes version metadata to `1.1.0-rc3`; final release remains pending Windows RC3 validation.

## v1.1.0 RC2 — 2026-09-15

UX-polish release candidate following the Windows PASS of v1.1.0 RC1.

- Dismisses the temporary `Ctrl+L` Location Box when the user interacts with the List/Grid file view; the mouse gesture continues normally and the Breadcrumb is restored.
- Softens the Selection Anchor indicator for lower-resolution displays: dark gray 1 px stroke with a more widely spaced dashed pattern.
- Adds **Rubber-band autoscroll speed** to Settings with a configurable range of **30–150** and default **100**.
- Keeps the validated near-edge acceleration curve; the chosen setting controls the maximum speed.
- Promotes version metadata to `1.1.0-rc2`; final release remains pending Windows RC2 validation.


## v1.1.0 RC1 — 2026-09-15

Release candidate for the v1.1.0 selection/navigation update.

- Adds Explorer-style rubber-band/marquee selection in List and Grid views.
- Adds validated Ctrl/Shift/Ctrl+Shift rubber-band behavior, edge autoscroll, List/Grid selection synchronization, and Selection Anchor visualization.
- Fixes selected-item activation from rapid scrollbar double-clicks.
- Keeps keyboard focus/current item aligned for Ferry-owned row/tile whitespace clicks and Shift range endpoints.
- `Ctrl+L` can now be exited with `Esc` or a second `Ctrl+L`, restoring Breadcrumb view without clearing the current file selection.
- Promotes version metadata to `1.1.0-rc1`; final release remains pending Windows RC validation.

## v1.0.2 — 2026-09-13

Ferry v1.0.2 replaces the previous Windows-delegated ZIP workflow with a Ferry-owned ZIP workflow built on .NET `System.IO.Compression`.

### ZIP progress / control

- Determinate overall progress in the bottom status area.
- Processed/total data, file count, speed, ETA when available, and Cancel.
- Setup window closes after Start; Ferry remains usable during archive work.
- One archive job per Ferry window at a time.

### Extraction conflicts

- Folder conflicts: MERGE / KEEP BOTH / SKIP / CANCEL.
- File conflicts: REPLACE / KEEP BOTH / SKIP / CANCEL.
- KEEP BOTH uses `name(1)` / `name(1).ext`, then incrementing suffixes.
- After MERGE, a file decision can optionally be remembered for the remaining file conflicts under that merged folder only.

### ZIP safety

- Blocks unsafe destination escape/path traversal, rooted paths, ADS-style names, reserved Windows device-name forms, and unsafe reparse-point merge/replace cases.
- Warns for expanded data >20 GiB, >50,000 files, or compression ratio >100×; the user chooses YES/NO.
- Uses temporary output files before finalizing extracted files.
- Safe cancellation/partial-result reporting and safe Ferry close while archive work is active.

### Validation

- Integrated ZIP Prototype 4 passed Windows regression and safety tests before release packaging.
- The accepted RC1 application logic is promoted unchanged to v1.0.2 final; finalization changes release metadata/documentation only.

## v1.0.1 — 2026-09-12

Ferry v1.0.1 focuses on small daily-workflow improvements and lighter List-view behavior.

### Rename / creation

- Single-item `F2` / Rename now edits the item name inline instead of opening a separate rename window.
- Files initially select only the filename stem while leaving the extension visible and editable.
- **New Folder** creates the folder in the stable bottom tail, scrolls it into view, selects it, and immediately enters inline rename.

### Search

- Filename search now ignores full-width / half-width differences while preserving kana-type distinctions.
- Examples: `カタカナ` matches `ｶﾀｶﾅ`, `ガ` matches `ｶﾞ`, and `ABC` matches `ＡＢＣ`.

### Sidebar

- Pinned folders can be reordered by drag and drop and retain their order after restart.
- The Pinned section now uses a dedicated ordered list model for stable reordering.
- Sidebar width is configurable from **50–480** and remains persisted.
- Sidebar and file view now meet at one visual boundary with no dedicated splitter gap; the transparent resize hit target remains easy to grab.

### List / Grid

- List view uses lightweight Windows Shell type icons rather than content thumbnails.
- Grid view continues to load thumbnails when visual previews are useful.
- List icon/type queries avoid unnecessary access to file content/thumbnail providers.

### Settings / About

- Settings now includes a visually separated **About Ferry** section showing the assembly-derived version, `Created by kazu0m1`, the GitHub repository, and MIT license information.

### Archive feedback

- ZIP compression and extraction now show a persistent neutral-gray indeterminate progress indicator in the bottom status bar.
- The archive progress indicator remains visible across folder/tab navigation until the operation finishes.
- Successful completion briefly shows `ZIP compression complete.` / `ZIP extraction complete.` before returning to the normal status.
- Archive work continues to use the Windows-provided archive tool rather than a Ferry-owned codec.

### Compatibility

- Existing v1.0.0 selection behavior, multi-item Enter/D&D, F12 terminal shortcut, external-update stable-tail behavior, and Windows detailed context menu are retained.
- **Open with…** behavior is unchanged in v1.0.1.

## v1.0.0 — First public release

Ferry v1.0.0 is the first public release of the lightweight Windows 11 file manager inspired by the simplicity and workflow of GNOME Files (Nautilus).

### Navigation / views

- Home defaults to `%USERPROFILE%`.
- List and Grid views with global sort/view behavior.
- Folder direct-child item counts.
- Tabs with `Ctrl+T`, `Ctrl+W`, `Ctrl+Tab`, and `Ctrl+Shift+Tab`.
- Natural filename sorting and visible ascending/descending indicators.
- **Sort folders before files** enabled by default.
- New items detected from external filesystem activity remain in a stable tail at the bottom until an explicit user sort/refresh.
- Same-path metadata such as active download size/time updates in place without automatic resorting.

### Selection / open / drag

- Native WPF Extended selection: normal click, `Ctrl+Click`, `Shift+Click`, and `Ctrl+A`.
- Clicking true empty List/Grid space clears selection.
- Multiple selected items can be opened with `Enter`.
- Dragging one already-selected item preserves and drags the complete selected set.
- Rubber-band/marquee selection is intentionally deferred from v1.0 in favor of the stable native selection model.

### Search

- Recursive progressive filename search in the current folder and descendants.
- Contains / StartsWith modes, `*` and `?` wildcards, and multiple-term AND matching.
- Windows Search is used when useful, with direct traversal as fallback.
- Fast exit back to the cached normal folder view.

### Rename

- Single-file `F2` rename with the complete filename visible and stem initially selected.
- Bulk Find & Replace and numbering templates with configurable start number and live preview.
- Collision-safe two-phase execution and extension preservation for bulk rename.
- Session-scoped `Ctrl+Z` for the last Ferry rename, invalidated after unrelated file mutations.

### Windows integration / file operations

- Copy / Cut / Paste, Recycle Bin delete, permanent delete, Properties, Open With, shortcuts, icons, thumbnails, and drag & drop.
- In-app Recycle Bin virtual view with Restore, Delete Permanently, Open Original Location, and Empty Recycle Bin.
- Detailed Windows Shell context menu, including multiple-selection support through both Show more options and `Shift+Right-click`.
- Open in Explorer with folder/file-aware targeting.
- Optional per-user **Open with Ferry** Explorer registration.
- ZIP Compress / Extract commands delegated to Windows 11.

### Terminal

- Machine-neutral Terminal **Auto** mode: Windows Terminal → Windows PowerShell → Command Prompt.
- Custom terminal command/arguments remain configurable.
- `F12` opens a terminal in the current Ferry folder regardless of item selection.

### Settings / privacy

- Portable JSON settings.
- Missing or invalid `settings.json` falls back safely to factory defaults; saving regenerates valid JSON.
- No telemetry, automatic crash upload, account system, Ferry-owned search database, or always-running service.

### Release validation

- Final Windows smoke test: **19 / 19 PASS** on the v1.0.0 RC15 code baseline.
- The final v1.0.0 source changes only release/version metadata and public documentation from that validated code baseline.
