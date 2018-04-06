namespace EasyHosts
{
    public enum MessageType
    {
        None = 0,
        Success = 1,
        Information = 2,
        Warning = 3,
        Failure = 4,

        /* The below special message types are reserved for now */
        Sorry = 10,
        Happy = 11,
        Beta = 12
    }

}