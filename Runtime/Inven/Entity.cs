using System.Collections.Generic;
using UnityEngine;

namespace Sindy.Inven
{
    public class Entity : ScriptableObject
    {
        public int id;
        public string nameId;
        public string descriptionId;
        public Sprite icon;
        public List<Entity> tags;

        public bool HasTag(Entity entity)
        {
            if (tags == null || tags.Count == 0)
            {
                return false;
            }

            foreach (var tag in tags)
            {
                if (tag.id == entity.id)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasTag(int id)
        {
            if (tags == null || tags.Count == 0)
            {
                return false;
            }

            foreach (var tag in tags)
            {
                if (tag.id == id)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasTag(string name)
        {
            if (tags == null || tags.Count == 0)
            {
                return false;
            }

            foreach (var tag in tags)
            {
                if (tag.nameId == name)
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return nameId;
        }
    }
}
