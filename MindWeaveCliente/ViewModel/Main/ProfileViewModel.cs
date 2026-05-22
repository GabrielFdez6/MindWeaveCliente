using MindWeaveClient.ProfileService;
using MindWeaveClient.Properties.Langs;
using MindWeaveClient.Services;
using MindWeaveClient.Services.Abstractions;
using MindWeaveClient.Utilities.Abstractions;
using MindWeaveClient.View.Main;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MindWeaveClient.ViewModel.Main
{
    public class ProfileViewModel : BaseViewModel, IDisposable
    {
        private const string DEFAULT_AVATAR_PATH = "/Resources/Images/Avatar/default_avatar.png";

        private readonly IProfileService profileService;
        private readonly IServiceExceptionHandler exceptionHandler;

        private bool isDisposed;
        private string welcomeMessage;
        private string username;
        private string avatarSource;
        private string firstName;
        private string lastName;
        private string dateOfBirth;
        private string gender;
        private int puzzlesCompleted;
        private int puzzlesWon;
        private string totalPlaytime;
        private int highestScore;

        public ObservableCollection<AchievementDto> AchievementsList { get; set; }
        public ObservableCollection<SocialMediaItem> SocialMediaList { get; set; }

        public string WelcomeMessage { get => welcomeMessage; set { welcomeMessage = value; OnPropertyChanged(); } }
        public string Username { get => username; set { username = value; OnPropertyChanged(); } }
        public string AvatarSource { get => avatarSource; set { avatarSource = value; OnPropertyChanged(); } }
        public string FirstName { get => firstName; set { firstName = value; OnPropertyChanged(); } }
        public string LastName { get => lastName; set { lastName = value; OnPropertyChanged(); } }
        public string DateOfBirth { get => dateOfBirth; set { dateOfBirth = value; OnPropertyChanged(); } }
        public string Gender { get => gender; set { gender = value; OnPropertyChanged(); } }
        public int PuzzlesCompleted { get => puzzlesCompleted; set { puzzlesCompleted = value; OnPropertyChanged(); } }
        public int PuzzlesWon { get => puzzlesWon; set { puzzlesWon = value; OnPropertyChanged(); } }
        public string TotalPlaytime { get => totalPlaytime; set { totalPlaytime = value; OnPropertyChanged(); } }
        public int HighestScore { get => highestScore; set { highestScore = value; OnPropertyChanged(); } }

        public ICommand BackCommand { get; }
        public ICommand EditProfileCommand { get; }

        public ProfileViewModel(
            IProfileService profileService,
            IDialogService dialogService,
            INavigationService navigationService,
            IServiceExceptionHandler exceptionHandler)
        {
            this.profileService = profileService;
            var navigationService1 = navigationService;
            this.exceptionHandler = exceptionHandler;

            AchievementsList = new ObservableCollection<AchievementDto>();
            SocialMediaList = new ObservableCollection<SocialMediaItem>();

            SessionService.AvatarPathChanged += OnAvatarPathChanged;

            BackCommand = new RelayCommand(p => navigationService1.goBack(), p => !IsBusy);
            EditProfileCommand = new RelayCommand(p => navigationService1.navigateTo<EditProfilePage>(), p => !IsBusy);

            Username = SessionService.Username ?? Lang.GlobalLbLoading;
            WelcomeMessage = $"{Lang.ProfileLbHi.TrimEnd('!')} {Username.ToUpper()}!";
            AvatarSource = SessionService.AvatarPath ?? DEFAULT_AVATAR_PATH;

            _ = loadProfileDataAsync();
            _ = loadAchievementsAsync();
        }

        private void OnAvatarPathChanged(object sender, EventArgs e)
        {
            AvatarSource = SessionService.AvatarPath ?? DEFAULT_AVATAR_PATH;
        }

        private async Task loadProfileDataAsync()
        {
            if (string.IsNullOrEmpty(SessionService.Username))
            {
                return;
            }

            SetBusy(true);
            try
            {
                PlayerProfileViewDto profileData = await profileService.getPlayerProfileViewAsync(SessionService.Username);

                if (profileData != null)
                {
                    updateProfileDisplay(profileData);
                }
            }
            catch (Exception ex)
            {
                exceptionHandler.handleException(ex, Lang.LoadProfileOperation);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void updateProfileDisplay(PlayerProfileViewDto profileData)
        {
            Username = profileData.Username;
            WelcomeMessage = $"{Lang.ProfileLbHi.TrimEnd('!')} {profileData.Username.ToUpper()}!";
            AvatarSource = profileData.AvatarPath ?? DEFAULT_AVATAR_PATH;

            FirstName = profileData.FirstName ?? Lang.GlobalLbNotSpecified;
            LastName = profileData.LastName ?? Lang.GlobalLbNotSpecified;
            DateOfBirth = profileData.DateOfBirth?.ToString("dd/MM/yyyy") ?? Lang.GlobalLbNotSpecified;
            Gender = profileData.Gender ?? Lang.GlobalLbNotSpecified;

            if (profileData.Stats != null)
            {
                PuzzlesCompleted = profileData.Stats.PuzzlesCompleted;
                PuzzlesWon = profileData.Stats.PuzzlesWon;
                TotalPlaytime = $"{profileData.Stats.TotalPlaytime.Hours}H {profileData.Stats.TotalPlaytime.Minutes}m";
                HighestScore = profileData.Stats.HighestScore;
            }

            SocialMediaList.Clear();
            if (profileData.SocialMedia != null)
            {
                foreach (var socialDto in profileData.SocialMedia.Where(s => !string.IsNullOrWhiteSpace(s.Username)))
                {
                    SocialMediaList.Add(new SocialMediaItem
                    {
                        IdSocialMediaPlatform = socialDto.IdSocialMediaPlatform,
                        PlatformName = socialDto.PlatformName,
                        Username = socialDto.Username
                    });
                }
            }
        }

        private async Task loadAchievementsAsync()
        {
            if (SessionService.PlayerId <= 0) return;

            try
            {
                AchievementDto[] achievementList = await profileService.getPlayerAchievementsAsync(SessionService.PlayerId);

                AchievementsList.Clear();
                foreach (var achievement in achievementList)
                {
                    string fileName = System.IO.Path.GetFileName(achievement.IconPath);
                    achievement.IconPath = $"pack://application:,,,/MindWeaveClient;component/Resources/Images/achievements/{fileName}";
                    AchievementsList.Add(achievement);
                }
            }
            catch (Exception ex)
            {
                exceptionHandler.handleException(ex, Lang.LoadAchievementsOperation);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                SessionService.AvatarPathChanged -= OnAvatarPathChanged;
            }

            isDisposed = true;
        }
    }
}