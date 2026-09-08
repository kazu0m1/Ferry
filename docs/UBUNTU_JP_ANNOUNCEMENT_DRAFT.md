# Ubuntu日本語コミュニティ向け 紹介文ドラフト

## タイトル案

WindowsでNautilusの操作感が恋しくて、ファイルマネージャー「Ferry」を作りました

## 本文案

Ubuntu / GNOME環境でFiles（Nautilus）を使っているうちに、そのシンプルで直接的なファイル操作がとても気に入りました。

一方でWindowsへ戻ると、特に「フォルダー内のアイテム数がすぐ見えること」「現在フォルダー以下を自然に検索できること」「複数ファイルを手早く一括リネームできること」が恋しくなりました。

そこで、Nautilusそのものを移植するのではなく、その使い心地から着想を得たWindows 11向けの軽量ファイルマネージャー **Ferry** を作りました。

Ferry v1.0.0では、次のような機能があります。

- List / Grid表示とフォルダー内アイテム数
- 現在フォルダー以下の再帰ファイル名検索
- Find & Replace / 連番 / 開始番号指定に対応した一括リネーム
- タブ
- Windowsのごみ箱をFerry内で表示
- ZIP圧縮・展開
- `Ctrl+Click` / `Shift+Click`による複数選択
- 複数選択したアイテムの一括Open / D&D
- `F12`で現在フォルダーにTerminalを開くショートカット
- Windows Shellの機能を可能な限り再利用する軽量設計

FerryはNautilusのportやforkではなく、Windows向けの独立実装です。ソースはMIT Licenseで公開しています。

LinuxとWindowsを行き来していて、同じようにNautilusの操作感が恋しい方がいれば、試していただけると嬉しいです。Issueや感想も歓迎します。

GitHub: https://github.com/kazu0m1/Ferry

---

※ GNOME®はGNOME Foundationの登録商標です。FerryはGNOME Foundationとの提携・承認・支援関係にはありません。
