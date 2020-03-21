using MonkeyFinder.Model;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MonkeyFinder.ViewModel
{
    public class MonkeyDetailsViewModel : BaseViewModel
    {
        public Command OpenMapCommand { get; }
        public Command MakeFavoriteCommand { get; }

        public MonkeyDetailsViewModel()
        {
            OpenMapCommand = new Command(async () => await OpenMapAsync());
            MakeFavoriteCommand = new Command(async () => await MakeFavoriteAsync());
        }

        public MonkeyDetailsViewModel(Monkey monkey)
            : this()
        {
            Monkey = monkey;
            Title = $"{Monkey.Name} Details";
        }
        Monkey monkey;
        public Monkey Monkey
        {
            get => monkey;
            set
            {
                if (monkey == value)
                    return;

                monkey = value;
                OnPropertyChanged();
            }
        }

        async Task OpenMapAsync()
        {
            try
            {
                await Map.OpenAsync(Monkey.Latitude, Monkey.Longitude);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to launch maps: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error, no Maps app!", ex.Message, "OK");
            }
        }

        async Task MakeFavoriteAsync()
        {
            try
            {
                var data = new DataService();

                var favorite = new FavoriteMonkey { MonkeyName = Monkey.Name };

                await data.SaveItem(Monkey.Name, favorite);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
