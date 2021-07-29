﻿using System.Collections.Generic;

namespace Rat.Data.Entities
{
    public class ProjectEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public ProjectTypeEntity Type { get; set; }

        public ICollection<UserEntity> Users { get; set; } = new List<UserEntity>();
    }
}
