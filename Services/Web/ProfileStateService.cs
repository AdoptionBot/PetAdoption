namespace PetAdoption.Services.Web
{
    public class ProfileStateService
    {
        public event Action? OnProfileCompleted;

        public void NotifyProfileCompleted()
        {
            OnProfileCompleted?.Invoke();
        }
    }
}