# 🚀 C# Modernization Plan: WinForms to .NET 8 Web API

## 🏛️ Project A: Legacy to Cloud-Native Modernization

### 1. 負債の再現（ドメイン：製造業向け受注管理）
あえて「救いようのない既存コード」を1画面分作成し、リプレイスの有効性を証明する。
* **ターゲット:** `LegacyWinFormsApp/FormOrder.cs`
* **アンチパターンの意図的実装:**
    * **SQL直書き:** Buttonイベント内に `SELECT * FROM Orders` を文字列で記述。
    * **密結合:** UIコントロールがビジネスロジックを直接保持（計算結果をそのままラベルに代入）。
    * **完全同期処理:** `Thread.Sleep` や重いSQL実行で画面がフリーズする仕様。
    * **設定のハードコード:** 接続文字列をソースコード内に直接記述。

### 2. モダン・リフォーム（.NET 8 LTS 採用）
2026年時点のエンタープライズ標準である .NET 8 を用い、クラウド前提の構成へ刷新する。
* **API層:** ASP.NET Core Minimal API (.NET 8)
    * ボイラープレートを排除し、コードの可読性を最大化。
* **フロントエンド:** React (Vite) + Tailwind CSS
    * 「現場のタブレット対応」を想定したレスポンシブな受注入力画面。
* **疎結合化の徹底 (DI):**
    * `Microsoft.Extensions.DependencyInjection` を使用。
    * リポジトリパターンを導入し、DBアクセスをUI（APIエンドポイント）から分離。
* **データアクセス:** Entity Framework Core 8
    * PostgreSQL を採用。マイグレーション機能によるスキーマ管理。
* **非同期化:** 全てのI/Oを `Task` (async/await) で実装し、スケーラビリティを確保。

### 3. 証明とデモ（README 駆動開発）
面談で「何を変えたか」を一瞬で理解させるための資産化。
* **比較図解:**
    * Before: SQL-in-Code, Monolithic, Sync.
    * After: Web API, Clean Architecture, Async.
* **移行の3原則:**
    1. **DI導入:** `new` 演算子を排除し、テスト可能な設計へ。
    2. **Repository化:** ビジネスロジックからSQL（データ操作）を隠蔽。
    3. **Async化:** パフォーマンスとユーザー体験の向上。
* **ポータビリティ:**
    * `docker-compose.yml` により、DB・API・Frontendをコマンド一つで立ち上げ。
    * xUnit による「正常系受注登録」のインテグレーションテスト1本を実装。

---

## 🛠️ Technology Stack Summary
| 項目 | 採用技術 |
| :--- | :--- |
| **Language** | C# 12 |
| **Platform** | .NET 8 (LTS) |
| **Framework** | ASP.NET Core Minimal API / React |
| **ORM** | Entity Framework Core 8 (PostgreSQL) |
| **Container** | Docker / Docker Compose |
| **Test** | xUnit |
