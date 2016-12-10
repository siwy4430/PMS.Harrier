﻿using System.Collections.Generic;
using PMS.Harrier.DataAccessLayer.Models;
using PMS.Harrier.DataAccessLayer.Repository.AbstractRepository;

namespace PMS.Harrier.DataAccessLayer.Repository.Interfaces
{
    public interface IProjectRepository : IRepository<Project>
    {
        IEnumerable<Models.Project> GetAllProjects();
        void GetProjectsWithProjectManagers();

        Models.Project GetProjectByName(string name);
    }
}