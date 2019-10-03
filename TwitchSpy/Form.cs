using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users;
using Newtonsoft.Json;
using TwitchLib.Api.Helix.Models.Streams;

namespace TwitchSpy
{
    public partial class Form : System.Windows.Forms.Form
    {
        private TwitchAPI API;
        //追隨的人
        private List<Follow> allFollows;
        //有開台的人
        private List<Stream> liveStreams;
        private List<TwitchLib.Api.V5.Models.Users.User> liveUserData;
        GetUsersFollowsResponse responseData;

        public Form()
        {
            InitializeComponent();
            init();
            
            ResetItems();
            GetFollowing();
        }

        private void Timer_TimesUp(object sender, System.Timers.ElapsedEventArgs e)
        {

        }

        public async Task GetFollowing()
        {
            await GetAllFollows();
            await GetFollowsOnLiveAsync();
            await GetOnLiveUserData();
            UpdateListView();
        }

        private void init()
        {
            API = new TwitchAPI();
            API.Settings.ClientId = ConfigurationManager.AppSettings["client_id"];

            InitialListView();

            timer1.Tick += new EventHandler(timer1_Tick);
        }

        private void ResetItems()
        {
            allFollows = new List<Follow>();
            liveStreams = new List<Stream>();
            liveUserData = new List<TwitchLib.Api.V5.Models.Users.User>();
        }

        /// <summary>
        /// 取得使用者追隨的帳號
        /// </summary>
        /// <returns></returns>
        private async Task GetAllFollows()
        {
            while (true)
            {
                responseData = await API.Helix.Users.GetUsersFollowsAsync((responseData?.Pagination?.Cursor?.ToString() ?? null), null, 100, "18678591");
                if (responseData != null)
                {
                    if (responseData.Pagination.Cursor == null) break;
                    allFollows.AddRange(responseData.Follows.ToList());
                }
            }
        }

        /// <summary>
        /// 檢查allFollows中哪些有開台
        /// </summary>
        /// <returns></returns>
        private async Task GetFollowsOnLiveAsync()
        {
            try
            {
                var getParams = new List<KeyValuePair<string, string>>();
                for (int i = 0; i < (Math.Ceiling(((double)allFollows.Count()/100))); i++)
                {
                    List<string> userIds = allFollows.Select(f => f.ToUserId).Skip(i*100).Take(100).ToList();
                    liveStreams.AddRange((await API.Helix.Streams.GetStreamsAsync(first: 100, userIds: userIds)).Streams.ToList());
                }
            }
            catch(Exception ex)
            {
                ErrorMethod(ex);
            }
        }

        /// <summary>
        /// 取得liveUsers內使用者資料
        /// </summary>
        /// <returns></returns>
        private async Task GetOnLiveUserData()
        {
            try
            {
                var tasks = liveStreams.Select(async f =>
                {
                    liveUserData.Add(await API.V5.Users.GetUserByIDAsync(f.UserId));
                });
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                ErrorMethod(ex);
            }
        }

        private void InitialListView()
        {
            listView1.View = View.Details;
            listView1.GridLines = true;
            listView1.LabelEdit = false;
            listView1.FullRowSelect = true;
            this.listView1.Columns.Add("ID", 100);
            this.listView1.Columns.Add("Name", 100);
            this.listView1.Columns.Add("標題",265);
        }

        private void UpdateListView()
        {
            this.listView1.BeginUpdate();
            this.listView1.Items.Clear();
            foreach (var f in liveUserData)
            {
                var stream = liveStreams.FirstOrDefault(x => x.UserId == f.Id);
                var i = new ListViewItem();
                i.Text = f.Name;
                i.SubItems.Add(f.DisplayName);
                i.SubItems.Add(stream.Title);
                this.listView1.Items.Add(i);
            }
            this.listView1.EndUpdate();
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            ResetItems();
            GetFollowing();
        }

        private void timer1_Tick(object sender, System.EventArgs e)
        {
            ResetItems();
            GetFollowing();
        }

        private void ErrorMethod(Exception ex)
        {
            var a = ex;
        }
    }
}
