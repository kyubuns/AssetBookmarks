# Asset Bookmarks

## Git

- `.idea` はリポジトリで管理する。ルートの `.gitignore` へ追加したり、既存の設定ファイルを削除したりしない。
- `README.md` と `Assets/AssetBookmarks/README.md` の共通内容は同期して更新する。
- READMEは利用者向けに簡潔に保ち、機能、導入、操作、保存上の注意に絞る。実装方式、設計上の選定理由、テスト範囲は記載しない。

## UI

- 小さくドッキングしたウィンドウで多数のBookmarkを一覧できることを最優先し、ヘッダーと各行は1行の高密度表示に保つ。
- ヘッダーは左から即時検索、`+`、`Aa` の順とし、Bookmark件数は表示しない。狭幅では検索欄を先に縮め、両ボタンの最小幅と、矢印を除く領域での文字中央揃えを維持する。
- 追加はウィンドウ全面のドラッグ＆ドロップと、選択中のUnity Asset・外部ファイル・外部フォルダ・Webサイトを扱うコンパクトな `+` メニューの両方を提供する。
- 全体のEditモードは設けない。動作変更、Copy Path、移動、削除は項目ごとの右クリックメニューに置く。
- 右端グリップは `ListView` と `canStartDrag` を使う並べ替え専用、行本体はBookmark対象をUnity標準のドラッグpayloadとして外へ渡す。Asset種別ごとのdrop挙動は受け側へ委ね、Prefab専用処理は持たない。
- Small、Medium、Largeの表示設定で文字、アイコン、行高をまとめて変える。
- Editor背景は塗りつぶさずUnityのホスト背景を継承し、色が必要な要素にはUnity USSテーマ変数を使う。Dark/Light別の固定RGB値は持たない。

## Data and actions

- Unity内のファイルとフォルダはGUIDで保持し、移動・リネーム後のパスを解決する。Bookmark対象がAsset全体である限り `GlobalObjectId` は使わず、sub-object対応が必要になった時だけ再検討する。
- 新規Sceneの既定動作は **Open in Unity**、その他のUnity Assetは **Select in Project** とする。
- 外部ファイルとフォルダは既定アプリで開く。WebサイトはHTTP/HTTPSのみ受け付け、省略されたschemeには `https://` を補う。表示や可用性確認のためのネットワークアクセスは行わない。
- Bookmarkはプロジェクトの絶対パスでscopeした `EditorPrefs` に保存し、変更は即時保存する。

## Implementation

- UIはUI Toolkitで実装し、Unityが提供するAPIと標準挙動で解決できる処理を独自実装しない。機能追加後は重複、不要な状態、分岐を見直してコードを簡潔に保つ。
- 常駐ポーリングや毎フレーム処理を置かずイベント駆動にする。`ListView` の通常更新では行を再生成せず再利用し、表示、検索、存在確認だけのためにAsset本体をロードしない。
- Scene既定動作、GUIDリネーム追従、URL正規化、外向きドラッグpayloadのEditModeテストを維持する。Unity Player buildは不要で、Editorコンパイルとテストを検証する。
