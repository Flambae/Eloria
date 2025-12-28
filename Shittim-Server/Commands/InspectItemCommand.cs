using Schale.Data;
using Schale.FlatData;
using Shittim.Services.Client;
using System.Text;
using System.Linq;

namespace Shittim.Commands
{
    [CommandHandler("inspectitem", "Inspect item properties", "/inspectitem [name]")]
    internal class InspectItemCommand : Command
    {
        public InspectItemCommand(IClientConnection connection, string[] args, bool validate = true) : base(connection, args, validate) { }

        [Argument(0, @".*", "Item name", ArgumentFlags.None)]
        public string Name { get; set; }

        public override async Task Execute()
        {
            var query = Name.ToLower();
            
            var localizeEtc = connection.ExcelTableService.GetTable<LocalizeEtcExcelT>();
            var nameMap = localizeEtc.ToDictionary(x => x.Key, x => x.NameEn ?? x.NameKr ?? "Unknown");

            var items = connection.ExcelTableService.GetTable<ItemExcelT>();
            
            foreach (var item in items)
            {
                 if (nameMap.TryGetValue(item.LocalizeEtcId, out var name))
                 {
                     if (name.ToLower().Contains(query))
                     {
                         var sb = new StringBuilder();
                         sb.AppendLine($"Name: {name}");
                         sb.AppendLine($"ID: {item.Id}");
                         sb.AppendLine($"Category: {item.ItemCategory}");
                         sb.AppendLine($"ImmediateUse: {item.ImmediateUse}");
                         sb.AppendLine($"UsingResultParcelType: {item.UsingResultParcelType}");
                         sb.AppendLine($"UsingResultId: {item.UsingResultId}");
                         sb.AppendLine($"Tags: {string.Join(", ", item.Tags ?? new List<Tag>())}");
                         
                         await connection.SendChatMessage(sb.ToString());
                     }
                 }
            }
        }
    }
}
