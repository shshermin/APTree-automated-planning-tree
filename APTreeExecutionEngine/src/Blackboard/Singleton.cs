     public abstract class Singleton<T> : Singleton where T : class
    {
        static T _Instance = null;
        static bool _bInitialising = false;
        static readonly object _InstanceLock = new object();

        public static T Instance
        {
            get
            {
                lock (_InstanceLock)
                {
                    if (bIsQuitting)
                        return null;

                    if (_Instance != null)
                        return _Instance;

                    _bInitialising = true;
                    
                    // Create new instance if none exists
                    _Instance = Activator.CreateInstance<T>();
                    
                    _bInitialising = false;
                    return _Instance;
                }
            }
        }

        protected virtual void Initialize()
        {
            // Override this method for initialization logic
        }
    }

    public abstract class Singleton 
    {
        protected static bool bIsQuitting { get; private set; } = false;
/// <summary>
///  w
/// </summary>
        public static void Shutdown()
        {
            bIsQuitting = true;
        }

        public static void Initialize()
        {
            bIsQuitting = false;
        }
    }