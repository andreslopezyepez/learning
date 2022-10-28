using Concurrency.Models;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.WebRequestMethods;

namespace Concurrency
{
    public partial class Form1 : Form
    {
        private string _apiUrl;
        private HttpClient _httpClient;

        public Form1()
        {
            _apiUrl = "https://localhost:7261";
            _httpClient = new HttpClient();
            InitializeComponent();
        }
        
        private void changeToGit()
        {
            Console.WriteLine("new log");
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            loading.Show();

            //var result = TwoSumHash(new[] { 3,2,4 }, 6);
            var r = RemoveParentheses("lee(t(c)o)de)");

            var table = new Hashtable
            {
                { "A1", new Card() },
                { "A2", "Value 2" },
                { "A3", 3 }
            };

            foreach (DictionaryEntry item in table)
                Console.WriteLine($"Key: {item.Key} - Value: {item.Value}");

            var cards = await GetCards(5);
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                await ProcessCards(cards);
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"{ex.Message}");
            }

            MessageBox.Show($"Finished in: {stopWatch.ElapsedMilliseconds / 1000} seconds");

            loading.Hide();
        }

        public int[] TwoSum(int[] nums, int target)
        {
            var numsDictionary = new Dictionary<int, int>();

            var complement = 0;
            for (var i = 0; i < nums.Length; i++)
            {
                complement = target - nums[i];
                var index = 0;
                if (complement > 0 && numsDictionary.TryGetValue(complement, out index))
                {
                    return new int[] { index, i };
                }
                else if (!numsDictionary.ContainsKey(nums[i]))
                {
                    numsDictionary.Add(nums[i], i);
                }
            }

            return null;
        }

        public int[] TwoSumHash(int[] nums, int target)
        {
            var table = new Hashtable();
            for (int i = 0; i < nums.Length; i++)
            {
                if (table.ContainsKey(target - nums[i]))
                {
                    return new int[] { (int)table[target - nums[i]], i };
                }
                table[nums[i]] = i;
            }
            return new int[] { -1, -1 };
        }

        public string RemoveParentheses(string s)
        {
            var sb = new StringBuilder();
            int open = 0;
            foreach (char c in s)
            {
                if (c == '(')
                {
                    open++;
                }
                else if (c== ')')
                {
                    if (open == 0) continue;
                    open--;
                }
                sb.Append(c);
            }

            var sb2 = new StringBuilder();
            for (int i = sb.Length - 1; i >= 0; i--)
            {
                if (sb[i] == '(' && open-- > 0) continue;
                sb2.Append(sb[i]);
            }

            return sb2.ToString().Reverse().ToString();
        }


        private async Task<List<string>> GetCards(int cardsAmount)
        {
            return await Task.Run(() =>
            {
                List<string> cards = new List<string>();

                for (int i = 0; i < cardsAmount; i++)
                {
                    cards.Add(i.ToString().PadLeft(16, '0'));
                }

                return cards;
            });
        }

        private async Task ProcessCards(List<string> cards)
        {
            using var semaphore = new SemaphoreSlim(2);

            var tasks = new List<Task<HttpResponseMessage>>();

            tasks = cards.Select(async card =>
            {
                var json = JsonConvert.SerializeObject(card);
                var content = new StringContent(json, encoding: Encoding.UTF8, "application/json");
                await semaphore.WaitAsync();
                try
                {
                    return await _httpClient.PostAsync($"{_apiUrl}/api/card", content);
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            var responses = await Task.WhenAll(tasks);

            var rejectedCards = new List<string>();

            foreach (var r in responses)
            {
                var content = await r.Content.ReadAsStringAsync();
                var cardResponse = JsonConvert.DeserializeObject<Card>(content);

                if (!cardResponse.Approved)
                    rejectedCards.Add(cardResponse.Name);
            }

            foreach (var card in rejectedCards)
                Console.WriteLine(card);
        }

        private async Task Wait()
        {
            await Task.Delay(TimeSpan.FromSeconds(0));
        }

        private async Task<string> Request()
        {
            using (var context = await _httpClient.GetAsync($"{_apiUrl}"))
            {
                context.EnsureSuccessStatusCode();
                return await context.Content.ReadAsStringAsync();
            }
        }
    }
}
