using Microsoft.EntityFrameworkCore;
using Shittim.Services.Client;
using Schale.Data.GameModel;
using Schale.FlatData;
using Schale.MX.GameLogic.Parcel;

namespace Shittim.Commands
{
    [CommandHandler("mail", "Command to sending mail to player", "/mail [type] [id,...] [amount]")]
    internal class MailCommand : Command
    {
        public MailCommand(IClientConnection connection, string[] args, bool validate = true) : base(connection, args, validate) { }

        [Argument(0, @".*", "Mail Type or 'help'", ArgumentFlags.None)]
        public string TypeStr { get; set; }

        [Argument(1, @".*", "Mail id(s) list", ArgumentFlags.IgnoreCase | ArgumentFlags.Optional)]
        public string IdStr { get; set; }

        [Argument(2, @".*", "amount", ArgumentFlags.IgnoreCase | ArgumentFlags.Optional)]
        public string AmountStr { get; set; } = "1";

        public record ItemKey(ParcelType Type, long Id);
        private static Dictionary<ItemKey, object> _itemDict;
        private static bool _initialized = false;

        public override async Task Execute()
        {
            InitializeIndex();
            using var context = await connection.Context.CreateDbContextAsync();
            var account = context.GetAccount(connection.AccountServerId);

            if (args.Length == 0 || args[0].ToLower() == "help")
            {
                await ShowHelp();
                return;
            }
        
            if (args[0].ToLower() == "clear")
            {
                int affectedRows = await context.Mails.Where(x => x.AccountServerId == account.ServerId && x.ReceiptDate == null)
                    .ExecuteDeleteAsync();
                if (affectedRows > 0)
                    await connection.SendChatMessage($"Deleted {affectedRows} unread mail");
                else
                    await connection.SendChatMessage("No emails to delete");

                return;
            }

            // Syntax: /mail [id] [amount] OR /mail [type] [id] [amount]
            // We try to detect if args[0] is an ID directly.
            
            long id = 0;
            long amount = 1;
            ParcelType? detectedType = null;
            bool isSimpleSyntax = long.TryParse(args[0], out id);

            if (isSimpleSyntax)
            {
                // Simple Syntax: /mail 1001 10
                if (args.Length > 1) long.TryParse(args[1], out amount);
                
                detectedType = FindTypeById(id);
                if (detectedType == null)
                {
                    await connection.SendChatMessage($"Error: Could not find item with ID {id}");
                    return;
                }
            }
            else
            {
                // Legacy/Explicit Syntax: /mail item 1001 10
                if (args.Length < 2) 
                {
                    await ShowHelp();
                    return;
                }
                
                if (!Enum.TryParse(args[0], true, out ParcelType pType))
                {
                     // Try parsing number for type
                     if (int.TryParse(args[0], out int typeInt)) pType = (ParcelType)typeInt;
                     else 
                     {
                         await connection.SendChatMessage("Error: Invalid type");
                         return;
                     }
                }
                detectedType = pType;
                long.TryParse(args[1], out id);
                if (args.Length > 2) long.TryParse(args[2], out amount);
            }

            var parcelInfos = new List<ParcelInfo> 
            { 
                new ParcelInfo { Key = new ParcelKeyPair { Type = detectedType.Value, Id = id }, Amount = amount } 
            };

            await connection.MailManager.SendSystemMail(
                account,
                "Schale",
                "Items sent by GM",
                parcelInfos,
                DateTime.Now.AddDays(7)
            );

            await connection.SendChatMessage($"Sent {amount}x {detectedType} (ID: {id}) via mail!");
            await connection.SendChatMessage("Please check your mailbox.");
        }

        private ParcelType? FindTypeById(long id)
        {
            // Priority check or look in dictionary
            // We can iterate the _itemDict keys matching the ID
            foreach(var key in _itemDict.Keys)
            {
                if (key.Id == id) return key.Type;
            }
            return null;
        }

        private void InitializeIndex()
        {
            if (!_initialized)
            {
                _itemDict = new Dictionary<ItemKey, object>();

                var currencyExcel = connection.ExcelTableService.GetTable<CurrencyExcelT>();
                foreach (var x in currencyExcel) _itemDict[new ItemKey(ParcelType.Currency, x.ID)] = x;

                var itemExcel = connection.ExcelTableService.GetTable<ItemExcelT>();
                foreach (var x in itemExcel) _itemDict[new ItemKey(ParcelType.Item, x.Id)] = x;

                var equipmentExcel = connection.ExcelTableService.GetTable<EquipmentExcelT>();
                foreach (var x in equipmentExcel) _itemDict[new ItemKey(ParcelType.Equipment, x.Id)] = x;
                
                var furnitureExcel = connection.ExcelTableService.GetTable<FurnitureExcelT>();
                foreach (var x in furnitureExcel) _itemDict[new ItemKey(ParcelType.Furniture, x.Id)] = x;
                
                var charExcel = connection.ExcelTableService.GetTable<CharacterExcelT>();
                foreach (var x in charExcel) _itemDict[new ItemKey(ParcelType.Character, x.Id)] = x;

                _initialized = true;
            }
        }

        private async Task ShowHelp()
        {
            await connection.SendChatMessage("/mail - Command to sending mail to player");
            await connection.SendChatMessage("Usage: /mail [type] [id,...] [amount]");
            await connection.SendChatMessage("Type: currency | equipment | item |  furniture - 2/3/4/13");
            await connection.SendChatMessage("Support sending multiple items of the same type, use ',' to separate each ID");
            await connection.SendChatMessage("You can find item ID at schaledb.com");
            await connection.SendChatMessage("If the client abnormal after sending email, use '/mail clear' to fix it.");
        }
    }
}
