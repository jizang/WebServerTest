// 這是 "using" 宣告，意思是「我要使用這個工具箱」。
// 就像你在組裝模型前，會先把所有需要的工具（螺絲起子、膠水）拿出來。
// 這些 using 讓我們能使用 .NET Core 提供的各種內建功能。

// 提供了 MVC 框架的核心功能，例如 Controller 基底類別、[HttpGet]、[HttpPost]、IActionResult、View() 等。
using Microsoft.AspNetCore.Mvc;
// 提供了 Entity Framework Core (EF Core) 的功能，這是用來跟資料庫溝通的工具 (ORM)。
// 讓我們可以用 C# 物件來操作資料庫，而不用寫一堆 SQL 語法。
using Microsoft.EntityFrameworkCore;
// 提供了密碼學相關功能，我們將用它來進行密碼雜湊 (Hashing)。
using System.Security.Cryptography;
// 提供了文字編碼（例如 UTF-8）的功能，在進行雜湊時需要將字串轉換為位元組(bytes)。
using System.Text;
// 引入我們自己定義的 "ViewModels"。ViewModel 是專門用來在 Controller 和 View 之間傳遞資料的「樣板」。
using WebServer.Models.ViewModels;
// 引入我們資料庫的 "Models"。這是 EF Core 用來對應資料庫表格的 C# 類別。
using WebServer.Models.WebServerDB;

// "namespace" 是一個「命名空間」，像是一個資料夾，用來組織和分類我們的程式碼。
// 這裡我們把這個 Controller 放在 WebServer.Controllers 這個資料夾裡。
namespace WebServer.Controllers
{
    // "public class AccountController : Controller"
    // "public": 表示這個類別(class)可以被專案中的其他程式碼存取。
    // "class AccountController": 這是我們類別的名稱。在 MVC 中，Controller 負責接收網頁請求、處理邏輯。
    // ": Controller": 這表示 AccountController「繼承」自 ASP.NET 內建的 Controller 類別。
    // 繼承就像是「我是一個更厲害的 Controller」，我擁有 Controller 所有的基本功能（例如 View()、RedirectToAction()）。
    public class AccountController : Controller
    {
        // 這是一個「私有唯讀欄位」(private readonly field)，用來儲存資料庫的連線上下文。
        // "private": 表示這個 _context 變數只能在這個 AccountController 類別內部使用。
        // "readonly": 表示這個 _context 變數一旦在「建構子」(Constructor) 中被賦值後，就不能再被修改。
        // "WebServerDBContext": 這是 EF Core 的「資料庫上下文」，它是我們與資料庫溝通的唯一窗口。
        private readonly WebServerDBContext _context;

        // 1. 透過依賴注入 (DI) 取得資料庫上下文 (DbContext)
        // 這是「建構子」(Constructor)，一個與類別同名的特殊方法。
        // 當 ASP.NET Core 要建立一個 AccountController 的「實體」(instance) 來處理請求時，會第一個呼叫它。
        public AccountController(WebServerDBContext context)
        {
            // (WebServerDBContext context): 這裡我們「要求」 ASP.NET Core 必須提供一個 WebServerDBContext 實體。
            // 這就是「依賴注入」(Dependency Injection, DI)。
            // 我們不需要自己去 new 一個資料庫連線，而是由系統（在 Program.cs 或 Startup.cs 中設定好的）自動「注入」給我們。
            // 這樣做的好處是「解耦合」，讓 Controller 更專注於邏輯，且更容易進行單元測試。

            // _context = context;
            // 我們將系統注入的 context，存到上面宣告的私有欄位 _context 中，
            // 這樣這個類別中的其他方法（例如 Signup）才能使用它。
            _context = context;
        }

        // ==========================================
        // == 註冊功能 (Signup)
        // ==========================================

        /// <summary>
        /// GET: /Account/Signup
        /// 顯示註冊表單頁面
        /// </summary>
        // "[HttpGet]": 這是一個「屬性」(Attribute)，用來告訴 ASP.NET Core：
        // 這個方法（Action）只應該回應「HTTP GET」請求。
        // HTTP GET 通常是用戶第一次造訪頁面（例如在瀏覽器輸入網址或點擊連結）時發出的。
        [HttpGet]
        public IActionResult Signup()
        {
            // "IActionResult": 這是一個「介面」，表示這個 Action 會回傳一個「結果」。
            // 這個結果可以是 HTML 頁面 (View)、重新導向 (Redirect)、JSON 資料或錯誤碼。

            // "return View(...)": 這個動作會去尋找對應的 View 檔案。
            // 預設路徑會是 /Views/Account/Signup.cshtml
            // "new SignupViewModel()": 我們建立一個「新的、空的」ViewModel。
            // 為什麼要傳一個空的？因為 View (Signup.cshtml) 裡的表單欄位（例如 <input asp-for="Account">）
            // 需要「繫結」(bind) 到一個 ViewModel 物件。如果不傳，View 在轉譯時會出錯。
            return View(new SignupViewModel());
        }

        /// <summary>
        /// POST: /Account/Signup
        /// 處理註冊表單提交
        /// </summary>
        // "[HttpPost]": 這個屬性告訴 ASP.NET Core：
        // 這個方法（Action）只應該回應「HTTP POST」請求。
        // HTTP POST 通常是在用戶填完表單並按下「提交」(Submit) 按鈕時發出的。
        [HttpPost]
        // "[ValidateAntiForgeryToken]": 這是一個非常重要的安全性屬性，用來「防止 CSRF 攻擊」。
        // (Cross-Site Request Forgery, 跨站請求偽造)
        // 它的運作方式：
        // 1. [HttpGet] 的 View() 在產生頁面時，會產生一個隱藏的「權杖」(Token)。
        // 2. 當用戶提交表單 [HttpPost] 時，這個權杖會一起被送回來。
        // 3. [ValidateAntiForgeryToken] 會自動檢查這個權杖是否正確且未過期。
        // 4. 如果是從其他惡意網站偽造的請求，它將沒有這個權杖，請求會被阻擋。
        [ValidateAntiForgeryToken]
        // "public async Task<IActionResult> Signup(SignupViewModel model)"
        // "async Task<...>": "async" 和 "Task" 表示這是一個「非同步」方法。
        // "非同步" (Asynchronous) 的意思是：當我們在等待資料庫回應時 (I/O 操作)，
        // 伺服器不會「卡住」不動，而是可以先去處理其他用戶的請求，
        // 這能大幅提升伺服器的處理效能和吞吐量。
        // "Signup(SignupViewModel model)": 這是 MVC 的「模型繫結」(Model Binding)。
        // 當表單 POST 回來時，MVC 會自動讀取表單中的資料（例如 "Account"、"Password"），
        // 並將這些值「自動填入」一個新的 SignupViewModel 物件（也就是 "model"）。
        public async Task<IActionResult> Signup(SignupViewModel model)
        {
            // "ModelState.IsValid":
            // "ModelState" 是一個內建屬性，它會儲存所有「驗證」的狀態。
            // "IsValid" 會檢查我們在 SignupViewModel.cs 類別中定義的所有「資料驗證」規則
            // (例如 [Required] 必填, [EmailAddress] 格式, [Compare] 密碼確認) 是否都通過了。
            // 這是由 MVC 框架自動完成的。
            if (ModelState.IsValid)
            {
                // 如果所有驗證都通過了，才開始執行註冊的商業邏輯。
                // "try...catch" 區塊：這是用來做「例外處理」(Error Handling) 的。
                // 我們「嘗試」(try) 執行資料庫相關的危險操作。
                try
                {
                    // --- 檢查帳號是否重複 ---
                    // "model.Account.Trim()": "Trim()" 會移除字串前後的空白（例如 "  user123  " -> "user123"）。
                    // ".ToUpper()": "ToUpper()" 會將字串轉為全大寫（例如 "user123" -> "USER123"）。
                    // 為什麼要轉大寫？ 這是為了「標準化」(Normalize)。
                    // 這樣在比對時，"user123" 和 "User123" 都會被視為 "USER123"，
                    // 確保帳號在不分大小寫的情況下是唯一的。
                    var accountUpper = model.Account.Trim().ToUpper();

                    // "await _context.User.AnyAsync(...)":
                    // "await": 關鍵字，表示我們要「等待」這個非同步操作完成，但不會卡住伺服器。
                    // "_context.User": 存取資料庫中的 "User" 資料表。
                    // ".AnyAsync()": 這是 EF Core 的 LINQ 方法，它會翻譯成 SQL。
                    // 意思是「是否存在任何 (Any) 一筆資料...」。
                    // "u => u.AccountNormalize == accountUpper":
                    // "..." 其條件是：資料庫中的 "AccountNormalize" 欄位值 == 我們剛剛轉換的大寫帳號。
                    // 這樣查詢效能最好，因為我們只要求資料庫回傳 true/false，而不是撈回整筆資料。
                    if (await _context.User.AnyAsync(u => u.AccountNormalize == accountUpper))
                    {
                        // "ModelState.AddModelError(...)": 如果帳號已存在，我們手動新增一個錯誤訊息到 ModelState。
                        // "nameof(model.ErrorMessage)": 這是 C# 6 的功能，它會安全地取得 "ErrorMessage" 這個屬性名稱。
                        // (假設 SignupViewModel 中有定義一個 ErrorMessage 屬性來顯示一般錯誤)
                        ModelState.AddModelError(nameof(model.ErrorMessage), "此帳號已被註冊");

                        // "return View(model)":
                        // **重要**：我們「返回註冊頁面」，並且「把 model 傳回去」。
                        // 這樣做的好處是：
                        // 1. 用戶之前填寫的資料（例如 Email、Name）都還會保留在表單上，不用重填。
                        // 2. View 上的驗證標籤（例如 <div asp-validation-summary>）會自動顯示我們新增的錯誤訊息。
                        return View(model);
                    }

                    // --- 檢查 Email 是否重複 (邏輯同上) ---
                    var emailUpper = model.Email.Trim().ToUpper();
                    if (await _context.User.AnyAsync(u => u.EmailNormalize == emailUpper))
                    {
                        ModelState.AddModelError(nameof(model.ErrorMessage), "此電子信箱已被註冊");
                        return View(model);
                    }

                    // --- 建立新的 User 實體 ---
                    // "var newUser = new User { ... };"
                    // 我們現在建立一個 "User" 物件。注意：
                    // - "SignupViewModel" 是「表單用的模型」。
                    // - "User" 是「資料庫表格用的模型」(Entity Model)。
                    // 我們必須手動將 ViewModel 的資料，對應到 Entity Model 上。
                    var newUser = new User
                    {
                        ID = Guid.NewGuid(), // 產生一個新的、全球唯一的 ID (UUID/GUID)。
                        Account = model.Account.Trim(), // 儲存使用者輸入的原始帳號 (包含大小寫)。
                        AccountNormalize = accountUpper, // 儲存大寫版本，供未來查詢/比對使用。
                        Email = model.Email.Trim(),
                        EmailNormalize = emailUpper, // 儲存大寫版本，供未來查詢/比對使用。
                        Name = model.Name.Trim(),
                        Birthday = model.Birthday, // 直接對應。
                        Mobile = model.Mobile?.Trim(), // "Mobile?" 的 "?" 表示這是 "可為 null 的字串"。
                                                       // 如果 model.Mobile 是 null，?. (Null-conditional operator) 會直接回傳 null，
                                                       // 而不會觸發 .Trim() 導致錯誤。

                        // --- 密碼雜湊 (Hashing) ---
                        // **極度重要**：絕對、絕對、絕對不可以直接儲存使用者的「明文密碼」！
                        // 我們呼叫下面的 EncoderSHA512 輔助方法，將密碼轉換成一串不可逆的雜湊值。
                        // "model.Password!": "!" 符號是「Null 容忍運算子」。
                        // 我們告訴編譯器：「雖然 Password 理論上可為 null，但我保證 ModelState.IsValid 已經檢查過，
                        // 所以在這裡它絕對不是 null，請不要發出警告。」
                        PasswordHash = EncoderSHA512(model.Password!),

                        // --- 設定預設值 ---
                        CreatedDT = DateTime.Now, // 紀錄帳號建立的當下時間。
                        LockoutEnabled = false, // 預設帳號不停用。
                        AccessFailedCount = 0 // 預設登入失敗次數為 0。
                    };

                    // --- 存入資料庫 ---
                    // "await _context.User.AddAsync(newUser);"
                    // 這行程式碼「還沒有」真的寫入資料庫。
                    // 它只是告訴 EF Core 的「上下文」(_context)：「我有一個新的 User 物件，請開始追蹤它，
                    // 並標記為『待新增』(Added) 狀態。」
                    await _context.User.AddAsync(newUser);

                    // "await _context.SaveChangesAsync();"
                    // **這行才是真正執行資料庫操作的地方**。
                    // EF Core 會檢查所有被追蹤的變更（包含 Add, Update, Delete），
                    // 產生對應的 SQL 語法（例如 INSERT INTO ...），
                    // 然後在一個「交易」(Transaction) 中，一次性地將所有變更送到資料庫執行。
                    // 因為這是 I/O 操作，所以我們使用 "await" 非同步等待它完成。
                    await _context.SaveChangesAsync();

                    // --- 註冊成功，重導向到登入頁面 ---
                    // "TempData": 是一種特殊的資料字典，用於在「重新導向」(Redirect) 之間傳遞「一次性」的訊息。
                    // 訊息只會在「下一次」請求中被讀取，讀取後就會自動清除。
                    // 這非常適合用來顯示「操作成功」的提示。
                    TempData["SuccessMessage"] = "註冊成功！請登入。";

                    // "return RedirectToAction("Signin");"
                    // 告知瀏覽器：「請你重新導向到 /Account/Signin 這個網址」。
                    // 這是處理 POST 請求成功的標準模式 (Post-Redirect-Get, PRG 模式)，
                    // 可以防止使用者按 F5 重新整理時，不小心重複提交表單。
                    return RedirectToAction("Signin");
                }
                catch (Exception ex)
                {
                    // "catch (Exception ex)": 如果 "try" 區塊中的任何程式碼（例如 SaveChangesAsync）
                    // 發生了「非預期」的錯誤（例如資料庫突然斷線），程式會跳到這裡。
                    // "ex" 變數會包含詳細的錯誤資訊。

                    // 我們將這個非預期的錯誤訊息，也加到 ModelState 中。
                    ModelState.AddModelError(nameof(model.ErrorMessage), "發生未知的錯誤：" + ex.Message);

                    // 同樣返回 View 並帶回 model，讓使用者知道發生了嚴重錯誤。
                    // (在正式環境中，你可能會記錄 "ex" 到日誌系統，並只顯示一個通用的錯誤訊息給用戶)
                    return View(model);
                }
            }

            // --- 如果 ModelState.IsValid == false ---
            // 如果程式執行到這裡，表示一開始 "ModelState.IsValid" 就是 false。
            // 這代表使用者的輸入不符合我們在 ViewModel 中定義的 DataAnnotation 規則
            // (例如「密碼為必填」、「Email 格式不符」或「兩次密碼輸入不一致」)。

            // 我們不需要做任何事，只需要 "return View(model)"。
            // MVC 框架會自動抓取 ModelState 中的所有驗證錯誤，
            // 並在 View (Signup.cshtml) 中對應的驗證標籤（例如 <span asp-validation-for="Password">）
            // 上顯示這些錯誤訊息。
            // 同時，因為我們傳回了 "model"，使用者已填寫的資料會被保留。
            return View(model);
        }

        // ==========================================
        // == 密碼雜湊輔助方法
        // ==========================================

        /// <summary>
        /// 將輸入字符串進行 SHA-512 編碼 (雜湊)
        /// </summary>
        // "private": 表示這個方法只能在 AccountController 內部被呼叫。
        // "static": 表示這個方法是「靜態的」。
        // 靜態方法不需要建立 AccountController 的「實體」就能呼叫。
        // 它也不允許存取類別中的「實體成員」（例如 _context）。
        // 這很適合用在這種「純粹的、功能性的」輔助方法上。
        private static string EncoderSHA512(string input)
        {
            // "if (string.IsNullOrEmpty(input))"
            // 這是一個「防呆」或「邊界檢查」(Guard Clause)。
            // 確保我們不會對一個 null 或空字串進行雜湊，那樣會引發錯誤。
            if (string.IsNullOrEmpty(input))
            {
                // "throw new ArgumentException(...)": 如果輸入有誤，我們拋出一個「例外」，
                // 這會中斷程式執行，並被上方的 "try...catch" 區塊捕捉到。
                throw new ArgumentException("Input cannot be null or empty.", nameof(input));
            }

            // "using (SHA512 sha512 = SHA512.Create())"
            // "using (...)": 這裡的 using 是「using 陳述式」。
            // "SHA512" 類別會使用到一些系統底層資源，"using" 能確保在區塊結束時，
            // 會自動呼叫 .Dispose() 方法來「釋放」這些資源，避免記憶體洩漏。
            // "SHA512.Create()": 建立一個 SHA512 雜湊演算法的實體。
            using (SHA512 sha512 = SHA512.Create())
            {
                // --- 步驟 1: 將字串轉為位元組陣列 (bytes) ---
                // 雜湊演算法是針對「位元組」(bytes) 運作的，不是「字串」(string)。
                // "Encoding.UTF8.GetBytes(input)": 我們使用標準的 UTF-8 編碼，
                // 將 C# 的字串（例如 "P@ssword"）轉換成一組位元組陣列。
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);

                // --- 步驟 2: 計算雜湊值 ---
                // "sha512.ComputeHash(inputBytes)":
                // 這是核心！它會執行 SHA-512 演算法，
                // 將輸入的位元組陣列 (inputBytes) 轉換成一個 64 位元組長 (512 bits) 的「雜湊值」陣列 (hashBytes)。
                // 這個過程是「單向的」，無法從 hashBytes 反推回 inputBytes。
                byte[] hashBytes = sha512.ComputeHash(inputBytes);

                // --- 步驟 3: 將位元組陣列轉換回「字串」以便儲存 ---
                // "StringBuilder sb = new StringBuilder();"
                // 我們不能直接將 byte[] 存入資料庫的字串欄位。
                // 我們需要將它轉換成「十六進位」(hexadecimal) 的字串表示法。
                // "StringBuilder" 是一個高效能的字串組合工具，
                // 在迴圈中組合字串時，效能遠高於 "string += ..."。
                StringBuilder sb = new StringBuilder();

                // 遍歷雜湊值 (hashBytes) 陣列中的每一個位元組 (byte)。
                foreach (byte b in hashBytes)
                {
                    // "sb.Append(b.ToString("x2"));"
                    // "b.ToString("x2")": 這是關鍵。
                    // "x2" 是一個格式化字串，意思是「將這個位元組(byte)轉換為 2 位數的十六進位字串」。
                    // 例如：
                    // byte 值 0   -> "00"
                    // byte 值 10  -> "0a"
                    // byte 值 255 -> "ff"
                    //
                    // SHA-512 產生 64 個 bytes，每個 byte 變成 2 個字元，
                    // 所以最終會得到一個 64 * 2 = 128 個字元長的十六進位字串。
                    sb.Append(b.ToString("x2"));
                }

                // 返回組合完成的 128 字元雜湊字串。
                return sb.ToString();

                // 補充：在現代化的應用程式中，更推薦使用「加鹽」(Salted) 的雜湊演算法，
                // 例如 ASP.NET Core Identity 內建的 PasswordHasher (使用 PBKDF2 或 Argon2)。
                // 「加鹽」可以更有效防止「彩虹表攻擊」(Rainbow Table Attacks)。
                // 但對於教學來說，SHA-512 是一個很清楚的「單向雜湊」範例。
            }
        }
    }
}
