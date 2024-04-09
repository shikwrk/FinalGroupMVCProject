using FinalGroupMVCPrj.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace FinalGroupMVCPrj.Hubs
{
    public class PushMsgHub :Hub
    {
        private readonly LifeShareLearnContext _context;
        public PushMsgHub(LifeShareLearnContext context)
        {
            _context = context;
        }
        private static List<string> ConList = new List<string>();
        
        private static Dictionary<string, string> PushStudentDict = new Dictionary<string, string>();

        private static Dictionary<string, string> adminDict = new Dictionary<string, string>();

        /// 連線事件
        public override async Task OnConnectedAsync()
        {
            if (ConList.Where(p => p == Context.ConnectionId).FirstOrDefault() == null)
            {
                ConList.Add(Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }


        public async Task SendPushStudentId(string memberId)
        {
            PushStudentDict[Context.ConnectionId] = memberId;

            await Clients.Caller.SendAsync("ConnectionEstablished", "Connection established successfully!");
        }


        public async Task SendAdminId(string adminId)
        {
            adminDict[Context.ConnectionId] = adminId;

            await Clients.Caller.SendAsync("ConnectionEstablished", "Connection established successfully!");
        }

        /// 離線事件
        public override async Task OnDisconnectedAsync(Exception ex)
        {
        }


        public async Task SendPushMsg(List<int> selectedMembers, string pushDelay)
        {
            int delaySeconds = Convert.ToInt32(pushDelay);

            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

            
            foreach (int memberId in selectedMembers)
            {
               
                if (PushStudentDict.ContainsValue(memberId.ToString()))
                {
                    
                    var connectionId = PushStudentDict.FirstOrDefault(x => x.Value == memberId.ToString()).Key;

                    if (connectionId != null)
                    {
                        await Clients.Client(connectionId).SendAsync("UploadNF");
                    }
                }
            }
        }
    }
}
