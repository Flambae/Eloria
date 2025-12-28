using Schale.Data;
using Schale.FlatData;
using Shittim.Services.Client;
using System.Text;

namespace Shittim.Commands
{
    [CommandHandler("search", "Search for items by name", "/search [name]")]
    internal class SearchCommand : Command
    {
        public SearchCommand(IClientConnection connection, string[] args, bool validate = true) : base(connection, args, validate) { }

        [Argument(0, @".*", "Search query", ArgumentFlags.None)]
        public string Query { get; set; }

        public override async Task Execute()
        {
            if (string.IsNullOrWhiteSpace(Query))
            {
                await connection.SendChatMessage("Usage: /search [name]");
                return;
            }

            var query = Query.ToLower();
            var matches = new List<string>();

            // Load Localization Table
            var localizeEtc = connection.ExcelTableService.GetTable<LocalizeEtcExcelT>();
            var nameMap = localizeEtc.ToDictionary(x => x.Key, x => x.NameEn ?? x.NameKr ?? "Unknown");

            // Search Items
            var items = connection.ExcelTableService.GetTable<ItemExcelT>();
            foreach (var item in items)
            {
                if (nameMap.TryGetValue(item.LocalizeEtcId, out var name))
                {
                    if (name.ToLower().Contains(query))
                    {
                        matches.Add($"[Item] {name} (ID: {item.Id})");
                    }
                }
            }

            // Search Currency (if applicable, structure might vary, adding basic check if LocalizeEtcId exists)
            // Currency usually has LocalizeEtcId too.
            try 
            {
                var currencies = connection.ExcelTableService.GetTable<CurrencyExcelT>();
                foreach (var currency in currencies)
                {
                     // CurrencyExcelT might have LocalizeEtcId, let's assume standard structure or verify later.
                     // For now, focusing on ItemExcel is safest as that's where Tickets usually are.
                }
            }
            catch {}


            if (matches.Count == 0)
            {
                await connection.SendChatMessage($"No items found matching '{Query}'");
            }
            else
            {
                await connection.SendChatMessage($"Found {matches.Count} items:");
                foreach (var match in matches.Take(10)) // Limit output
                {
                    await connection.SendChatMessage(match);
                }
                if (matches.Count > 10)
                    await connection.SendChatMessage($"...and {matches.Count - 10} more.");
            }
        }
    }
}
