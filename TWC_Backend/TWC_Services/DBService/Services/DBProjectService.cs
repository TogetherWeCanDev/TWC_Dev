﻿using Microsoft.EntityFrameworkCore;
using TWC_DatabaseLayer;
using TWC_DatabaseLayer.DTOs;
using TWC_DatabaseLayer.Models;
using TWC_Services.DBService.Interfaces;

namespace TWC_Services.DBService.Services
{
    public class DBProjectService : IDBProjectService
    {
        private DataContext _context;

        public DBProjectService(DataContext context) {
            _context = context; 
        }
        public async Task<Project> CreateProjectAsync(ProjectCreationDTO project)
        {
            try
            {
                Project newProject = new Project()
                {
                    Name = project.Name,
                    MaxMembers = project.MaxMembers,
                    Description = project.Description,
                    Members = new List<ProjectMember>(),
                    Tags = new List<ProjectTag>(),

                };

                await _context.Projects.AddAsync(newProject);
                await _context.SaveChangesAsync();

                User owner = await _context.Users.FindAsync(project.CreatorId);
                newProject.Members.Add(new ProjectMember()
                {
                    IsOwner = true,
                    ProjectId = newProject.Id,
                    UserId = owner.Id,
                });

                await _context.SaveChangesAsync();

                foreach (string tagDTO in project.Tags)
                {
                    Tag tag = await _context.Tags.SingleOrDefaultAsync(x => x.Name == tagDTO);
                    ProjectTag projectTag = new ProjectTag()
                    {
                        TagId = tag.Id,
                        ProjectId = newProject.Id,
                        Tag = tag,
                    };

                    newProject.Tags.Add(projectTag);
                }

                await _context.SaveChangesAsync();

                await _context.Entry(newProject).Collection(x => x.Tags).LoadAsync();
                await _context.Entry(newProject).Collection(x => x.Tags).LoadAsync();
                await _context.Entry(newProject).Collection(x => x.Members).LoadAsync();

                return newProject;
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception: {ex} in DBProjectService.cs in function CreateProjectAsync");
            }
        }

        public async void DeleteProjectAsync(int id)
        {
            Project project = await _context.Projects.FindAsync(id);
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return;
        }

        public async Task<Project> EditProjectAsync(Project project)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            return await _context.Projects.Include(x=>x.Tags).Include(y=> y.Members).ToListAsync();
        }

        public async Task<Project> GetProjectByNameAsync(string name)
        {
            try
            {
                Project project = await _context.Projects.SingleOrDefaultAsync(x => x.Name.Equals(name));

                return project;

            }
            catch (Exception ex)
            {
                throw new Exception($"cant found {typeof(Project).Name} by name. Messege error: " + ex.Message);
            }
        }

        public async Task<Project> GetProjectByIdAsync(int id)
        {
            try
            {
                Project project = await _context.Projects.SingleOrDefaultAsync(x => x.Id == id);

                return project;

            }
            catch (Exception ex)
            {
                throw new Exception($"cant found {typeof(Project).Name} by id. Messege error: " + ex.Message);
            }
        }
        private async Task<Project> AddMemberToProjectAsync(User user, Project project, bool isOwner)
        {
            ProjectMember existingMember = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == project.Id && pm.UserId == user.Id);

            if (existingMember != null)
            {
                return project;
            }

            ProjectMember newProjectMember = new ProjectMember
            {
                ProjectId = project.Id,
                UserId = user.Id,
                IsOwner = isOwner
            };

            await _context.ProjectMembers.AddAsync(newProjectMember);
            await _context.SaveChangesAsync();

            await _context.Entry(project).Collection(p => p.Members).LoadAsync();

            return project;
        }

        public async Task<Project> AddMemberToProjectAsync(User user, Project project)
        {
            return await AddMemberToProjectAsync(user, project, isOwner: false);
        }

        public async Task<Project> RemoveMemberFromProjectAsync(User user, Project project)
        {
            ProjectMember memberToRemove = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == project.Id && pm.UserId == user.Id);

            if(memberToRemove == null)
            {
                return project;
            }
            if(memberToRemove.IsOwner)
            {
                throw new Exception("You can not remove owner User from Project members");
            }

            _context.ProjectMembers.Remove(memberToRemove);
            await _context.SaveChangesAsync();

            await _context.Entry(project).Collection(p => p.Members).LoadAsync();

            return project;
        }
    }
}
