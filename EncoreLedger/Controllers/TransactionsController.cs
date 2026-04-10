using EncoreLedger.Models;
using EncoreLedger.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EncoreLedger.Models.ViewModels;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using System.Text.Json;


namespace EncoreLedger.Controllers
{
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            bool displayAccountName = false,
            string sortColumn = "Date",
            bool ascending = true)
        {
            var query = _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.Account)
                .AsQueryable();

            // Sorting
            query = sortColumn switch
            {
                "Category" => ascending
                    ? query.OrderBy(t => t.Category.Name)
                    : query.OrderByDescending(t => t.Category.Name),

                "Amount" => ascending
                    ? query.OrderBy(t => t.Amount)
                    : query.OrderByDescending(t => t.Amount),

                _ => ascending
                    ? query.OrderBy(t => t.TransactionDate)
                    : query.OrderByDescending(t => t.TransactionDate)
            };

            var transactions = await query.ToListAsync();

            var vm = new TransactionIndexViewModel
            {
                Transactions = transactions,
                DisplayAccountName = displayAccountName,
                SortColumn = sortColumn,
                Ascending = ascending,
                TotalTransactionCount = transactions.Count,
                LastDateUpdated = transactions.Any()
                    ? transactions.Max(t => t.DateEdited)
                    : null
            };

            // Added for bulk edit modal window
            ViewBag.Categories = new SelectList(_context.Categories, "IDCategory", "Name");
            ViewBag.Accounts = new SelectList(_context.Accounts, "IDAccount", "AccountName");

            return View(vm);
        }

        public IActionResult Create()
        {
            LoadDropdowns();
            return View();
        }

        [HttpPost]
        public IActionResult Create(Transaction transaction)
        {
            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return View(transaction);
            }

            transaction.DateCreated = DateTime.Now;
            transaction.DateEdited = DateTime.Now;

            _context.Add(transaction);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        /* TODO: Replace with ViewModel as time goes on? */
        private void LoadDropdowns()
        {
            ViewBag.Accounts = new SelectList(
                _context.Accounts.ToList(),
                "IDAccount",
                "AccountName"
            );

            ViewBag.Categories = new SelectList(
                _context.Categories.ToList(),
                "IDCategory",
                "Name"
            );

        }

        public async Task<IActionResult> Edit(int id)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.IDTransaction == id);

            if (transaction == null)
                return NotFound();

            LoadDropdowns();
            return View(transaction);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Transaction updated)
        {
            if (id != updated.IDTransaction)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return View(updated);
            }

            var existing = await _context.Transactions.FindAsync(id);

            if (existing == null)
                return NotFound();

            // Update fields that can be edited
            existing.Description = updated.Description;
            existing.Amount = updated.Amount;
            existing.Notes = updated.Notes;
            existing.TransactionDate = updated.TransactionDate;
            existing.CategoryID = updated.CategoryID;
            existing.AccountID = updated.AccountID;

            // Update the edited timestamp
            existing.DateEdited = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var transaction = _context.Transactions
                .FirstOrDefault(t => t.IDTransaction == id);

            if (transaction == null)
                return NotFound();

            _context.Transactions.Remove(transaction);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SelectDelete(int[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                var transactions = _context.Transactions
                    .Where(t => selectedIds.Contains(t.IDTransaction))
                    .ToList();

                _context.Transactions.RemoveRange(transactions);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Filter(
            bool displayAccountName,
            string sortColumn,
            bool ascending)
        {
            var query = _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.Account)
                .AsQueryable();

            query = sortColumn switch
            {
                "Category" => ascending
                    ? query.OrderBy(t => t.Category.Name)
                    : query.OrderByDescending(t => t.Category.Name),

                "Amount" => ascending
                    ? query.OrderBy(t => t.Amount)
                    : query.OrderByDescending(t => t.Amount),

                _ => ascending
                    ? query.OrderBy(t => t.TransactionDate)
                    : query.OrderByDescending(t => t.TransactionDate)
            };

            var vm = new TransactionIndexViewModel
            {
                Transactions = await query.ToListAsync(),
                DisplayAccountName = displayAccountName,
                SortColumn = sortColumn,
                Ascending = ascending
            };

            return PartialView("_TransactionTable", vm);
        }

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file, int accountId)
        {
            // Reject empty uploads
            // TODO: Check error code
            if (file == null || file.Length == 0)
                return View();

            /* Configure CSV parsing 
                1. No header row -- attempted to configure with a header row
                                    but concluded that with all the different
                                    ways banks format CSV files, this was simpler.
                2. Ignore blank lines
                3. Trim whitespace
            */
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim
            };

            var rows = new List<string[]>();

            // Read all rows from the uploaded CSV
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, config);

            while (await csv.ReadAsync())
            {
                rows.Add(csv.Context.Parser.Record);
            }

            // Need at least one row of data, plus the metadata row
            //      most banks have.
            if (rows.Count < 2)
                return View("Import");

            var headers = rows[0].ToList();

            // Show only a small preview of the data to the user
            var previewRows = rows.Skip(1).Take(5).ToList();

            // Load saved mappings for this account (and global mappings with no account)
            var savedMappings = await _context.ImportMappings
                .Where(m => m.AccountID == accountId || m.AccountID == null)
                .OrderByDescending(m => m.DateModified)
                .ToListAsync();

            var vm = new ImportPreviewViewModel
            {
                AccountID = accountId,
                Headers = headers,
                PreviewRows = previewRows,
                SerializedRows = JsonSerializer.Serialize(rows),
                FileName = file.FileName,
                SavedMappings = savedMappings
            };

            return View("ImportPreview", vm);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmImport(ImportPreviewViewModel model)
        {
            // If a saved mapping was selected, load it and apply it
            if (model.SelectedMappingID.HasValue && model.SelectedMappingID.Value > 0)
            {
                var selectedMapping = await _context.ImportMappings
                    .FirstOrDefaultAsync(m => m.IDImportMapping == model.SelectedMappingID.Value);

                if (selectedMapping != null)
                {
                    // Apply the saved mapping to ColumnMappings
                    model.ColumnMappings.Clear();
                    if (selectedMapping.DateIndex.HasValue)
                        model.ColumnMappings[selectedMapping.DateIndex.Value] = "Date";
                    if (selectedMapping.DescriptionIndex.HasValue)
                        model.ColumnMappings[selectedMapping.DescriptionIndex.Value] = "Description";
                    if (selectedMapping.AmountIndex.HasValue)
                        model.ColumnMappings[selectedMapping.AmountIndex.Value] = "Amount";
                    if (selectedMapping.DebitIndex.HasValue)
                        model.ColumnMappings[selectedMapping.DebitIndex.Value] = "Debit";
                    if (selectedMapping.CreditIndex.HasValue)
                        model.ColumnMappings[selectedMapping.CreditIndex.Value] = "Credit";
                }
            }

            // Get necessary information from preview
            var accountID = model.AccountID;
            var serializedRows = model.SerializedRows;
            var pendingHandling = model.PendingHandling;
            var columnMappings = model.ColumnMappings;

            var rows = JsonSerializer.Deserialize<List<string[]>>(serializedRows);

            if (rows == null || rows.Count < 2)
                return RedirectToAction("Index");

            // Use user input to determine 
            // which CSV column maps to which field
            int? dateIndex = null;
            int? descIndex = null;
            int? amountIndex = null;
            int? debitIndex = null;
            int? creditIndex = null;

            foreach (var mapping in columnMappings)
            {
                switch (mapping.Value)
                {
                    case "Date":
                        dateIndex = mapping.Key;
                        break;
                    case "Description":
                        descIndex = mapping.Key;
                        break;
                    case "Amount":
                        amountIndex = mapping.Key;
                        break;
                    case "Debit":
                        debitIndex = mapping.Key;
                        break;
                    case "Credit":
                        creditIndex = mapping.Key;
                        break;
                }
            }

            // Initialize the bulk import record
            var bulkImport = new BulkImport
            {
                FileName = model.FileName,
                ImportDate = DateTime.Now,
                TotalRecords = 0,
                RecordsImported = 0,
                RecordsFailed = 0,
                RecordsIgnored = 0,
                Transactions = new List<Transaction>()
            };

            // Process each data row (skip header)
            foreach (var row in rows.Skip(1))
            {
                // Tracks number of records in import file
                bulkImport.TotalRecords++;

                /* 
                   Cannot import without at least a date and a description
                   NOTE: Amount is also a required field, but many bank CSV files
                         use 'debit' and 'credit' fields to track positive and
                         negative values respectively, so "Amount" may not 
                         technically be indexed to.
                */
                if (!dateIndex.HasValue || !descIndex.HasValue)
                {
                    bulkImport.RecordsFailed++;
                    continue;
                }

                // Parse transaction date
                if (!DateTime.TryParse(row[dateIndex.Value], out var date))
                {
                    // Handle pending transactions according to user preference
                    // TODO: check how other banks store pending transactions
                    //        in their CSV files.
                    if (row[dateIndex.Value].Equals("pending", StringComparison.OrdinalIgnoreCase))
                    {
                        if (pendingHandling == "UseToday")
                        {
                            date = DateTime.Today;
                        }
                        else
                        {
                            bulkImport.RecordsIgnored++;
                            continue;
                        }
                    }
                    else
                    {
                        bulkImport.RecordsFailed++;
                        continue;
                    }
                }

                /* 
                   Parse transaction amount
                   First, check and see if "Amount" is indexed to. If
                     not, use "Credit"/"Debit" indexes to determine
                     Amount variable
                */
                decimal amount = 0;
                bool amountParsed = false;

                if (amountIndex.HasValue)
                {
                    // Single-column amount (may include parentheses for negatives)
                    var rawAmount = row[amountIndex.Value];

                    // Detect accounting-style negatives: (123.45)
                    bool isNegative = rawAmount.Contains("(") && rawAmount.Contains(")");

                    // Get just the number amount.
                    var cleaned = rawAmount
                        .Replace("$", "")
                        .Replace(",", "")
                        .Replace(" ", "")
                        .Replace("(", "")
                        .Replace(")", "");

                    amountParsed = decimal.TryParse(
                        cleaned,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out amount);

                    if (isNegative)
                        amount *= -1;
                }
                else
                {
                    // Debit/credit style import
                    decimal debit = 0, credit = 0;

                    bool debitParsed = debitIndex.HasValue &&
                        decimal.TryParse(
                            row[debitIndex.Value]
                            .Replace("$", "")
                            .Replace(",", "")
                            .Replace(" ", ""),
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out debit);

                    bool creditParsed = creditIndex.HasValue &&
                        decimal.TryParse(
                            row[creditIndex.Value]
                            .Replace("$", "")
                            .Replace(",", "")
                            .Replace(" ", ""),
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out credit);

                    amount = credit - debit;
                    amountParsed = debitParsed || creditParsed;
                }

                if (!amountParsed)
                {
                    bulkImport.RecordsFailed++;
                    continue;
                }

                Console.WriteLine($"RAW AMOUNT: '{row[amountIndex.Value]}'");

                // Build the transaction entity
                var transaction = new Transaction
                {
                    TransactionDate = date,
                    Description = row[descIndex.Value],
                    Amount = amount,
                    AccountID = null,
                    // AccountID = accountID,s
                    BulkImport = bulkImport,
                    DateCreated = DateTime.Now,
                    DateEdited = DateTime.Now
                };

                bulkImport.Transactions.Add(transaction);
                bulkImport.RecordsImported++;
            }

            // Save the import and all transactions
            _context.BulkImports.Add(bulkImport);
            await _context.SaveChangesAsync();

            // Save the mapping pattern if requested
            if (model.SaveMapping && !string.IsNullOrWhiteSpace(model.SaveMappingName))
            {
                var importMapping = new ImportMapping
                {
                    Name = model.SaveMappingName.Trim(),
                    AccountID = accountID > 0 ? accountID : null,
                    DateIndex = dateIndex,
                    DescriptionIndex = descIndex,
                    AmountIndex = amountIndex,
                    DebitIndex = debitIndex,
                    CreditIndex = creditIndex,
                    DateCreated = DateTime.Now,
                    DateModified = DateTime.Now
                };

                _context.ImportMappings.Add(importMapping);
                await _context.SaveChangesAsync();
            }

            // Provide user feedback
            // TODO: match error message to existing aesthetic
            TempData["ImportMessage"] =
                $"{bulkImport.RecordsImported} imported, {bulkImport.RecordsFailed} failed, " +
                $"{bulkImport.RecordsIgnored} ignored.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkEdit(BulkEditViewModel model)
        {
            if (model.SelectedIds == null || model.SelectedIds.Length == 0)
                return RedirectToAction(nameof(Index));

            var transactions = await _context.Transactions
                .Where(t => model.SelectedIds.Contains(t.IDTransaction))
                .ToListAsync();

            foreach (var t in transactions)
            {
                // Only update fields that have values provided
                if (model.CategoryId.HasValue)
                    t.CategoryID = model.CategoryId;

                if (model.AccountId.HasValue)
                    t.AccountID = model.AccountId;

                if (!string.IsNullOrWhiteSpace(model.Description))
                    t.Description = model.Description;

                if (!string.IsNullOrWhiteSpace(model.Notes))
                    t.Notes = model.Notes;

                t.DateEdited = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }
}