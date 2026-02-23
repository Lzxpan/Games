# Games - WinForms 俄羅斯方塊

這個專案是一個使用 **C# + Windows Forms (.NET 8)** 製作的單機版俄羅斯方塊（Tetris）範例。

- 專案進入點位於 `Program.cs`，啟動 `Form1` 視窗。
- 主要遊戲邏輯、輸入控制與繪圖渲染都在 `Form1.cs`。

---

## 1. 專案功能總覽

此遊戲已實作以下核心功能：

1. **基本遊戲規則**
   - 10x20 棋盤。
   - 7 種 Tetromino（I/J/L/O/S/T/Z）隨機生成。
   - 方塊自動下落、碰撞檢查、到底後鎖定。
   - 完整消行與上方方塊下移。

2. **分數與進度系統**
   - 軟降（`Down/S`）會加分。
   - 每次消行依消除行數與等級計分。
   - 每消 10 行提升等級，並提高下落速度。

3. **控制與狀態**
   - 左右移動：`←/→` 或 `A/D`
   - 旋轉：`↑` / `W` / `Space`
   - 加速下落（軟降）：`↓` 或 `S`
   - 暫停/繼續：`P`
   - 遊戲結束後重新開始：`Enter`

4. **畫面渲染**
   - 棋盤背景與格線。
   - 固定方塊與目前下落方塊。
   - 右側資訊面板（分數、消行、等級、下一個方塊、操作說明）。
   - 暫停與 Game Over 半透明遮罩提示。
   - 方塊鎖定時（落到底部或壓到其他方塊）的短暫畫面震動效果。

---

## 2. 專案檔案結構

- `Program.cs`
  - WinForms 應用程式入口。
- `Form1.cs`
  - 遊戲所有邏輯（資料結構、遊戲流程、輸入、繪圖）。
- `Form1.Designer.cs` / `Form1.resx`
  - WinForms 設計器產生內容。
- `Games.csproj`
  - .NET 8 Windows Forms 專案設定。

---

## 3. 主要資料與狀態（Form1）

`Form1` 使用以下關鍵欄位維持遊戲狀態：

- **棋盤設定常數**
  - `BoardWidth = 10`、`BoardHeight = 20`
  - `CellSize = 30`
- **UI 配置常數**
  - `SidePanelWidth`、`TopPadding`、`LeftPadding`
- **棋盤資料**
  - `board[BoardHeight, BoardWidth]`：0 代表空格，1~7 對應方塊種類。
- **方塊顏色**
  - `pieceColors`：索引 1~7 對應每種方塊顏色。
- **方塊形狀模板**
  - `pieceDefinitions`：7 組 `Point[]`，每組 4 個格子。
- **亂數與計時器**
  - `random`：生成隨機方塊。
  - `gameTimer`：主遊戲 Tick（`System.Windows.Forms.Timer`）。
- **目前與下一個方塊**
  - `currentPiece`、`nextPiece`（型別 `FallingPiece`）。
- **進度狀態**
  - `score`、`linesCleared`、`level`、`isGameOver`。

---

## 4. 遊戲生命週期（高層流程）

1. `Program.Main()` 啟動 `Form1`。
2. `Form1` 建構子設定視窗外觀、綁定 `Tick` / `KeyDown` 事件，呼叫 `StartNewGame()`。
3. `StartNewGame()` 重置棋盤與分數，產生 `nextPiece`，再 `SpawnNewPiece()`。
4. `gameTimer` 週期觸發 `GameTick()`，讓方塊每次往下移動一格。
5. 若不能再下降，`LockPiece()` 固定方塊、嘗試 `ClearLines()`，再生成下一塊。
6. 若新方塊無法放入起始位置，遊戲結束（`isGameOver = true`）。
7. 玩家可按 `Enter` 呼叫 `StartNewGame()` 重新開始。

---

## 5. 每個函式的功能與流程

以下以 `Form1.cs` 內函式逐一說明。

### 5.1 建構與初始化

#### `Form1()`
**功能：** 初始化視窗與遊戲啟動前設定。  
**流程：**
1. 呼叫 `InitializeComponent()`。
2. 設定標題、大小、背景色、文字色、`KeyPreview`、`DoubleBuffered`。
3. 綁定 `gameTimer.Tick` 到 `GameTick()`。
4. 綁定 `KeyDown` 到 `Form1_KeyDown()`。
5. 呼叫 `StartNewGame()`。

#### `StartNewGame()`
**功能：** 重新開始一局。  
**流程：**
1. 清空 `board`。
2. 重設 `score`、`linesCleared`、`level`、`isGameOver`。
3. 先產生 `nextPiece`。
4. 呼叫 `SpawnNewPiece()` 讓 `currentPiece` 進場。
5. 呼叫 `UpdateSpeed()` 設定 Tick 間隔。
6. 啟動 `gameTimer`，並 `Invalidate()` 觸發重繪。

#### `CreateRandomPiece()`
**功能：** 隨機建立一個 Tetromino。  
**流程：**
1. 隨機取 1~7 的型別。
2. 從 `pieceDefinitions` 複製該形狀座標。
3. 回傳新的 `FallingPiece(type, blocks, startPosition)`。

#### `SpawnNewPiece()`
**功能：** 把 `nextPiece` 變成目前下落方塊，並預產下一塊。  
**流程：**
1. `currentPiece = nextPiece`。
2. 把 `currentPiece.Position` 放到起始位置（X=3, Y=0）。
3. 生成新的 `nextPiece`。
4. 用 `CanPlace(...)` 檢查可否放置。
5. 若不可放置 → `isGameOver = true` 並停止計時器。

---

### 5.2 核心遊戲迴圈與動作

#### `GameTick()`
**功能：** 每次時間片推進遊戲。  
**流程：**
1. 若 `isGameOver`，直接返回。
2. 嘗試 `MovePiece(0, 1)`。
3. 如果不能下降，呼叫 `LockPiece()`。
4. `Invalidate()` 重繪。

#### `MovePiece(int dx, int dy)`
**功能：** 移動當前方塊。  
**流程：**
1. 計算目標座標 `target`。
2. `CanPlace(...)` 檢查是否可移動。
3. 可移動則更新 `currentPiece.Position` 並回傳 `true`。
4. 否則回傳 `false`。

#### `RotatePiece()`
**功能：** 旋轉目前方塊（O 方塊不旋轉）。  
**流程：**
1. 若遊戲結束或方塊型別為 O（type=4）則返回。
2. 對每個格子做旋轉座標轉換，得到 `rotated`。
3. 依序嘗試 `kicks = {0, -1, 1, -2, 2}` 的水平微調（簡化 wall kick）。
4. 第一個可放置位置成立時，更新方塊形狀與位置並重繪。

#### `HardDrop()`
**功能：** 讓方塊瞬間落到底（目前程式保留此函式，按鍵已改為不觸發）。  
**流程：**
1. 持續 `MovePiece(0,1)` 直到失敗。
2. 依落下距離加分（每格 2 分）。
3. 呼叫 `LockPiece()` 固定方塊並產生下一塊。
4. `Invalidate()` 重繪。

#### `LockPiece()`
**功能：** 將目前方塊寫入棋盤，處理消行、分數與等級，再出新方塊。  
**流程：**
1. 鎖定當下立即觸發 `StartLandingShake()`（無論是碰到底部或堆疊在其他方塊上）。
2. 走訪 `currentPiece.Blocks` 寫入 `board[y,x] = type`。
3. 呼叫 `ClearLines()` 取得本次消除行數。
4. 若有消行：
   - 累加 `linesCleared`。
   - 依 1/2/3/4 行給分（100/300/500/800 × level）。
   - 依總消行數計算新等級（每 10 行 +1）。
   - 若升級，呼叫 `UpdateSpeed()`。
5. 呼叫 `SpawnNewPiece()`。

#### `ClearLines()`
**功能：** 清除滿行並讓上方資料下落。  
**流程：**
1. 由底往上掃描每一列。
2. 若該列全滿：
   - 計數 `cleared++`。
   - 把上方每列整體下移一格。
   - 最頂列清為 0。
   - `y++` 重新檢查同一列（因為有新資料掉下來）。
3. 回傳清除總行數。

#### `CanPlace(FallingPiece piece, Point position, Point[] blocks)`
**功能：** 驗證方塊放置是否合法。  
**流程：**
1. 逐格計算棋盤座標。
2. 若超出左右邊界或底部，回傳 `false`。
3. 若格子落在棋盤範圍內且目標位置已有方塊，回傳 `false`。
4. 全部合法回傳 `true`。

#### `UpdateSpeed()`
**功能：** 依等級更新下落速度。  
**流程：**
1. 計算 `gameTimer.Interval = Max(80, 550 - (level - 1) * 45)`。
2. 等級越高，間隔越短。

#### `StartLandingShake()`
**功能：** 在方塊鎖定當下立即啟動震動畫面效果。  
**流程：**
1. 設定震動剩餘幀數與初始偏移。
2. 啟動 `shakeTimer`（若尚未啟動）。
3. 呼叫 `Invalidate()` 讓當前幀立刻重繪並出現震動。

#### `ShakeTimer_Tick(object? sender, EventArgs e)`
**功能：** 逐幀更新震動偏移，時間到後停止震動。  
**流程：**
1. 若幀數用完，清空偏移並停止 `shakeTimer`。
2. 否則遞減幀數並生成新的隨機偏移。
3. 每次 Tick 都 `Invalidate()` 觸發重繪。

---

### 5.3 輸入與狀態控制

#### `Form1_KeyDown(object? sender, KeyEventArgs e)`
**功能：** 處理所有按鍵操作。  
**流程：**
1. 若已 Game Over，只接受 `Enter` 重新開始。
2. 否則依按鍵觸發：
   - `Left/A`：左移
   - `Right/D`：右移
   - `Down/S`：軟降（成功則 +1 分）
   - `Up/W/Space`：旋轉
   - `P`：暫停/繼續
3. 最後 `Invalidate()`。

#### `TogglePause()`
**功能：** 切換暫停狀態。  
**流程：**
1. 若已 Game Over，直接返回。
2. `gameTimer.Enabled = !gameTimer.Enabled`。
3. `Invalidate()` 更新覆蓋提示。

---

### 5.4 繪圖渲染

#### `OnPaint(PaintEventArgs e)`
**功能：** 主畫面渲染入口。  
**流程：**
1. 設定抗鋸齒。
2. 若正在震動，先套用 `TranslateTransform` 偏移。
3. 依序畫：棋盤、目前方塊、右側面板。
4. 若暫停顯示「已暫停」遮罩。
5. 若 Game Over 顯示「遊戲結束」遮罩。

#### `DrawBoard(Graphics g)`
**功能：** 繪製棋盤背景、格線、已鎖定方塊。  
**流程：**
1. 畫棋盤底色矩形。
2. 畫垂直與水平格線。
3. 掃描 `board`，非 0 格呼叫 `DrawCell()`。

#### `DrawCurrentPiece(Graphics g)`
**功能：** 繪製正在下落的方塊。  
**流程：**
1. 若 Game Over 不畫。
2. 走訪 `currentPiece.Blocks`，對每格呼叫 `DrawCell()`。

#### `DrawCell(Graphics g, int gridX, int gridY, Color color)`
**功能：** 繪製單一方塊格（含漸層與邊框）。  
**流程：**
1. 換算成像素矩形。
2. 用線性漸層填色。
3. 繪製外框。

#### `DrawSidePanel(Graphics g)`
**功能：** 繪製右側資訊區。  
**流程：**
1. 顯示標題。
2. 顯示分數、消行、等級。
3. 顯示「下一個方塊」文字與預覽。
4. 顯示操作說明。

#### `DrawNextPiecePreview(Graphics g, int x, int y)`
**功能：** 繪製下一個方塊預覽框。  
**流程：**
1. 畫預覽邊框。
2. 逐格繪製 `nextPiece.Blocks`。

#### `DrawOverlay(Graphics g, string text)`
**功能：** 顯示半透明覆蓋提示（暫停/結束）。  
**流程：**
1. 畫半透明黑底。
2. 用 `TextRenderer.DrawText` 置中顯示訊息。

---

### 5.5 內部資料型別

#### `FallingPiece`（`private sealed class`）
**功能：** 封裝單一下落方塊狀態。  
**欄位/屬性：**
- `Type`：方塊種類（1~7）。
- `Blocks`：4 個相對座標。
- `Position`：此方塊在棋盤上的基準位置。

---

## 6. 實際執行時序範例（從生成到鎖定）

1. `SpawnNewPiece()` 將方塊放在上方。
2. 每次 `GameTick()` 呼叫 `MovePiece(0,1)`。
3. 玩家可透過 `KeyDown` 同步改變位置/旋轉。
4. 當 `MovePiece(0,1)` 失敗時，`LockPiece()` 把方塊寫進 `board`。
5. `ClearLines()` 清滿行並更新分數/等級。
6. 再次 `SpawnNewPiece()` 進入下一輪。

---

## 7. 已知限制與可延伸方向

### 已知限制
- 目前旋轉系統是簡化版 wall-kick，非完整 SRS。
- 隨機出塊使用純亂數，非 7-bag。
- 沒有「幽靈方塊（ghost piece）」與「Hold 機制」。
- 沒有音效、設定頁與持久化排行榜。

### 可延伸方向
- 改用 7-bag 提升出塊公平性。
- 實作完整 SRS 旋轉規則與踢牆表。
- 加入 Hold、Ghost、Combo、Back-to-back、T-Spin 計分。
- 新增開始選單、難度選擇、排行榜儲存。

---

## 8. 建置與執行

### Visual Studio 2022
1. 開啟 `Games.sln`。
2. 目標框架確認為 `.NET 8 (Windows)`。
3. 按 `F5` 執行。

### CLI
```bash
dotnet build
dotnet run
```

> 若終端機顯示 `dotnet: command not found`，請先安裝 .NET SDK 8。

---

## 9. 快速檢查清單（操作）

- 能左右移動：`←/→` 或 `A/D`
- 能旋轉：`↑/W/Space`
- 能軟降且加分：`↓/S`
- 能暫停/恢復：`P`
- Game Over 後可重開：`Enter`

