namespace PetAdoption.Services
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