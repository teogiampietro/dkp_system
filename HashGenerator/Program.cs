using System;
using Microsoft.AspNetCore.Identity;

var hasher = new PasswordHasher<object>();
var hash = hasher.HashPassword(null!, "Admin123!");
Console.WriteLine("Password hash for 'Admin123!':");
Console.WriteLine(hash);
