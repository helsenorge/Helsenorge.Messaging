## Release Process

1. Bump the version number in Directory.Build.props and commit this change
1. Tag the commit with the bumped version number.

   Tag should have the syntax [version-number], for example: 7.0.0.

   Example git command: `git tag -a 7.0.0`
1. Then run the command `git push --tags`
1. Go to https://github.com/helsenorge/Helsenorge.Messaging/tags to
   start the release process.
1. When the release is published the workflow nuget_deploy.yml is triggered and will
   publish the package to NuGet.

