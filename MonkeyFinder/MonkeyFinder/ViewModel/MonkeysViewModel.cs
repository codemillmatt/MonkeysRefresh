using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Essentials;

using System.Linq;
using MonkeyFinder.Model;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Collections.Generic;

namespace MonkeyFinder.ViewModel
{
    public class MonkeysViewModel : BaseViewModel
    {
        Random random = new Random();
        public Command GetMonkeysCommand { get; }
        public Command SignInCommand { get; }
        public ObservableCollection<Monkey> Monkeys { get; }

        bool isSignedIn;
        public bool IsSignedIn
        {
            get => isSignedIn;
            set
            {
                if (isSignedIn == value)
                    return;

                isSignedIn = value;

                OnPropertyChanged();
            }
        }

        public bool IsNotSignedIn => !IsSignedIn;

        public MonkeysViewModel()
        {
            Title = "Monkey Finder";
            Monkeys = new ObservableCollection<Monkey>();
            GetMonkeysCommand = new Command(async () => await GetMonkeysAsync());
            SignInCommand = new Command(async () => await SignInAsync());
            IsSignedIn = false;
        }

        HttpClient httpClient;
        HttpClient Client => httpClient ?? (httpClient = new HttpClient());

        DataService dataService;
        DataService DataClient => dataService ?? (dataService = new DataService());

        async Task GetMonkeysAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                List<Monkey> monkeys = null;

                var connection = DeviceInfo.Platform == DevicePlatform.watchOS ?
                    NetworkAccess.Internet : Connectivity.NetworkAccess;

                // if internet is working
                if (connection == NetworkAccess.Internet)
                {
                    //var json = await Client.GetStringAsync("https://montemagno.com/monkeys.json");

                    monkeys = await DataClient.GetAllItems<Monkey>(DocumentType.Public);

                    if (IsSignedIn)
                    {
                        var favorites = await DataClient.GetAllItems<FavoriteMonkey>(DocumentType.User);

                        foreach (var item in monkeys)
                        {
                            if (favorites.Any(m => m.MonkeyName == item.Name))
                                item.IsFavorite = true;
                        }
                    }
                }
                else
                {
                    monkeys = new List<Monkey>
                    {
                        new Monkey { Name = "Sample Monkey", Location = "Sample Monkey" },
                        new Monkey { Name = "Sample Monkey", Location = "Sample Monkey" },
                        new Monkey { Name = "Sample Monkey", Location = "Sample Monkey" }
                    };
                }

                Monkeys.Clear();
                foreach (var monkey in monkeys)
                    Monkeys.Add(monkey);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get monkeys: {ex.Message}");
                if (Application.Current?.MainPage == null)
                    return;

                await Application.Current.MainPage.DisplayAlert("Error!", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<string> GetRandomMonkey()
        {
            if (Monkeys.Count == 0)
                await GetMonkeysAsync();

            if (Monkeys.Count == 0)
                return string.Empty;

            var next = random.Next(0, Monkeys.Count);
            return Monkeys[next].Image;
        }

        public async Task SignInAsync()
        {
            try
            {
                var userContext = await AuthenticationService.Instance.SignInAsync();

                IsSignedIn = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                IsSignedIn = false;
            }


        }
    }
}
