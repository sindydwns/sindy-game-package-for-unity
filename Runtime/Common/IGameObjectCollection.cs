namespace Sindy.Common
{
    public interface IGameObjectCollection
    {
        public T GetGameObject<T>(string id) where T : UnityEngine.Object;
    }
}
