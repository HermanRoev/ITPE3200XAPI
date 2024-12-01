using ITPE3200XAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace ITPE3200XAPI.DAL
{
    public static class DbInit
    {
        public static void Seed(IApplicationBuilder app)
        {
            // Create a new scope to retrieve scoped services
            using var serviceScope = app.ApplicationServices.CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Optionally delete and recreate the database for testing (Comment out for testing)
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // Retrieve UserManager to create users
            var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Seed data if the database is empty
            if (!context.Users.Any())
            {
                // Create users
                var users = new List<ApplicationUser>
                {
                    new ApplicationUser { UserName = "user1@example.com", Email = "user1@example.com", ProfilePictureUrl = "/uploads/test3.jpg" },
                    new ApplicationUser { UserName = "user2", Email = "user2@example.com", ProfilePictureUrl = "/uploads/test4.jpg" },
                    new ApplicationUser { UserName = "user3", Email = "user3@example.com", ProfilePictureUrl = "/uploads/test5.jpg" }
                };

                foreach (var user in users)
                {
                    var result = userManager.CreateAsync(user, "Password123!").Result;
                    if (!result.Succeeded)
                    {
                        throw new Exception($"Failed to create user {user.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }

                // Retrieve created users
                var user1 = userManager.FindByNameAsync("user1@example.com").Result;
                var user2 = userManager.FindByNameAsync("user2").Result;
                var user3 = userManager.FindByNameAsync("user3").Result;

                // Create posts
                var posts = new List<Post>
                {
                    new Post(userId: user1!.Id, content: "This is the first test post. Here we need a lot longer text to test if the show more is working, there should be a show more button that can display more content when pressed") { CreatedAt = DateTime.UtcNow.AddMinutes(-30) },
                    new Post(userId: user2!.Id, content: "This is the second test post.") { CreatedAt = DateTime.UtcNow.AddMinutes(-20) },
                    new Post(userId: user3!.Id, content: "This is the third test post.") { CreatedAt = DateTime.UtcNow.AddMinutes(-10) }
                };
                context.Posts.AddRange(posts);
                context.SaveChanges();

                // Retrieve created posts
                var post1 = posts[0];
                var post2 = posts[1];
                var post3 = posts[2];

                // Create post images
                var postImages = new List<PostImage>
                {
                    // Post 1 with multiple images
                    new PostImage(postId: post1.PostId, imageUrl: "/uploads/test3.jpg") { CreatedAt = DateTime.UtcNow.AddMinutes(-60) },
                    new PostImage(postId: post1.PostId, imageUrl: "/uploads/test4.jpg") { CreatedAt = DateTime.UtcNow.AddMinutes(-50) },
                    new PostImage(postId: post1.PostId, imageUrl: "/uploads/test5.jpg") { CreatedAt = DateTime.UtcNow.AddMinutes(-40) },

                    // Post 2 with one image
                    new PostImage(postId: post2.PostId, imageUrl: "/uploads/test4.jpg") { CreatedAt = DateTime.UtcNow.AddMinutes(-30) },

                    // Post 3 with multiple images
                    new PostImage(postId: post3.PostId, imageUrl: "/uploads/test5.jpg") { CreatedAt = DateTime.UtcNow.AddMinutes(-20) },
                    new PostImage(postId: post3.PostId, imageUrl: "/uploads/test3.jpg") { CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                    new PostImage(postId: post3.PostId, imageUrl: "/uploads/test4.jpg") { CreatedAt = DateTime.UtcNow.AddMinutes(-5) }
                };
                context.PostImages.AddRange(postImages);
                context.SaveChanges();

                // Create comments
                var comments = new List<Comment>
                {
                    new Comment(postId: post1.PostId, userId: user2.Id, content: "Nice post!") { CreatedAt = DateTime.UtcNow.AddMinutes(-28) },
                    new Comment(postId: post1.PostId, userId: user3.Id, content: "I agree! Holy crap this is the best image quality i have ever seen. This must have been shot on an iphone 16 pro! That is the only phone that can shoot images like this!") { CreatedAt = DateTime.UtcNow.AddMinutes(-27) },
                    new Comment(postId: post2.PostId, userId: user1.Id, content: "Interesting thoughts.") { CreatedAt = DateTime.UtcNow.AddMinutes(-18) },
                    new Comment(postId: post3.PostId, userId: user1.Id, content: "Great pictures!") { CreatedAt = DateTime.UtcNow.AddMinutes(-8) }
                };
                context.Comments.AddRange(comments);
                context.SaveChanges();

                // Create likes
                var likes = new List<Like>
                {
                    new Like(postId: post1.PostId, userId: user2.Id) { CreatedAt = DateTime.UtcNow.AddMinutes(-28) },
                    new Like(postId: post1.PostId, userId: user3.Id) { CreatedAt = DateTime.UtcNow.AddMinutes(-27) },
                    new Like(postId: post2.PostId, userId: user1.Id) { CreatedAt = DateTime.UtcNow.AddMinutes(-18) },
                    new Like(postId: post3.PostId, userId: user1.Id) { CreatedAt = DateTime.UtcNow.AddMinutes(-8) },
                    new Like(postId: post3.PostId, userId: user2.Id) { CreatedAt = DateTime.UtcNow.AddMinutes(-7) }
                };
                context.Likes.AddRange(likes);
                context.SaveChanges();
            }
        }
    }
}