This is Wings reworked: [nexus](https://www.nexusmods.com/bladeandsorcery/mods/6144), [git](https://github.com/sjankowskim/wings)

It's kind of a mess to have a standard c# solution under a unity assets folder, since unity creates a .meta file for everything.  Unity also doesn't like the duplication of files between obj and bin.  But this way everything is part of the same repo

The .gitignore is a combination of unity and visual studio gitignores