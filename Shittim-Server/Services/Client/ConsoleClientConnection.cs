using Schale.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using BlueArchiveAPI.Services;
using Shittim.Services.IrcClient;
using Shittim_Server.Services;

namespace Shittim.Services.Client
{
    public class ConsoleClientConnection : IClientConnection
    {
        public IDbContextFactory<SchaleDataContext> Context { get; set; }
        public IMapper Mapper { get; set; }
        public ExcelTableService ExcelTableService { get; set; }
        public long AccountServerId { get; set; }
        public MailManager MailManager { get; set; }

        public StreamWriter StreamWriter { get; set; }

        public ConsoleClientConnection(
            IDbContextFactory<SchaleDataContext> _ctx,
            IMapper _mapper,
            ExcelTableService _excel,
            StreamWriter _writer,
            long _accountServerId,
            MailManager _mailManager
        )
        {
            Context = _ctx;
            Mapper = _mapper;
            ExcelTableService = _excel;
            StreamWriter = _writer;
            AccountServerId = _accountServerId;
            MailManager = _mailManager;
        }

        public Task SendChatMessage(string text)
        {
            StreamWriter.WriteLine($"[CMD] {text}");
            StreamWriter.Flush();
            return Task.CompletedTask;
        }

        public Task SendChatMessage(string text, string nickname, long pfpCharacterId, long stickerId, IrcMessageType messageType)
        {
            return Task.CompletedTask;
        }

        public Task SendEmote(long stickerId)
        {
            return Task.CompletedTask;
        }
    }
}
