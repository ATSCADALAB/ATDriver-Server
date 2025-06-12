using System.Collections.Generic;

namespace ATDriver_Server
{
    // Class quản lý phiên (Session) của các kết nối đến Service
    // Su dung Singleton
    public class ServiceRepository
    {
        #region FILEDS

        private static volatile ServiceRepository instance;

        private static readonly object keyLock = new object();

        #endregion

        #region PROPERTIES

        // Danh sách session đang duy trì kết nối
        public List<string> SessionIDs { get; private set; }

        // Session của InternalClient trên ATDriverServer
        // Với InternalClient, việc mã hóa được bỏ qua => tăng tốc độ đáp ứng
        public string InternalSessionID { get; set; }

        #endregion

        #region CONSTRUCTORS

        public static ServiceRepository Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (keyLock)
                    {
                        if (instance == null)
                        {
                            instance = new ServiceRepository();
                        }
                    }
                }
                return instance;
            }
        }

        private ServiceRepository()
        {
            SessionIDs = new List<string>();
        }

        #endregion
    }
}
