using MonkeyFinder.Model;
using MonkeyFinder.View;
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
        public Command FavoriteCommand { get; }

        string favoriteButtonText;
        public string FavoriteButtonText
        {
            get => favoriteButtonText;
            set
            {
                if (favoriteButtonText == value)
                    return;

                favoriteButtonText = value;

                OnPropertyChanged();
            }
        }

        MonkeyDetailsViewModel()
        {
            OpenMapCommand = new Command(async () => await OpenMapAsync());
            FavoriteCommand = new Command(async () => await HandleFavoritism());
        }

        public MonkeyDetailsViewModel(Monkey monkey)
            : this()
        {
            Monkey = monkey;
            Title = $"{Monkey.Name} Details";

            FavoriteButtonText = Monkey.IsFavorite ? "Delete Favorite" : "Make Favorite";
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

        async Task HandleFavoritism()
        {
            try
            {
                var data = new DataService();

                if (!Monkey.IsFavorite)
                {
                    // Favorite a monkey
                    var favorite = new FavoriteMonkey { MonkeyName = Monkey.Name };

                    await data.SaveItem(Monkey.Name, favorite);
                    FavoriteButtonText = "Delete Favorite";

                    await App.Current.MainPage.DisplayAlert("Saved", $"Yeah! I love you {Monkey.Name}!", "OK");
                }
                else
                {
                    // Unfavorite the monkey
                    await data.DeleteItem(Monkey.Name);
                    FavoriteButtonText = "Make Favorite";

                    await App.Current.MainPage.DisplayAlert("Deleted", $"I never liked {Monkey.Name} anyways", "OK");
                }                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            Monkey.IsFavorite = !Monkey.IsFavorite;
        }
    }
}
