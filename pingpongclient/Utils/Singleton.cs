namespace MyUtils
{
    public class Singleton<T> where T : Singleton<T>, new()
    {
        static T Instance;
        public static T GetInst
        {
            get
            {
                if (Instance == null)
                {
                    Instance = new T();
                    Instance.Init();
                }

                return Instance;
            }
        }

        protected virtual void Init()
        {

        }
    }
}