## Contributing to MIMWAL Code

The two ways to contribute to MIMWAL Code are: Fork the Repository or Submit an Issue.

### Fork the MIMWAL Repository.

Fork a copy of the [MIM Repo](https://github.com/Microsoft/MIMWAL), make the changes in your fork and send a pull request. Forking a repository allows you to freely experiment with changes without affecting the original project. See [Fork A Repo](https://help.github.com/articles/fork-a-repo/) help article for more information.

#### Coding Guidelines

When developing the code, make sure that you instrument your code adequately. And what's better way to know whether it's adequate or not than not using the debugger in the first place!! Not installing Visual Studio on your MIM server in a great way to avoid temptation to use debugger. 

As for the coding style, it's simple. Download and install [StyleCop](http://stylecop.codeplex.com/) and let it guide you. Note: At the time of writing, StyleCop does not support Visual Studio 2015 so you'll need to use Visual Studio 2013.

##### Code Acceptance

While code acceptance for a bug fix or a minor enhancement like adding a new function is likely to be very straightforward, the code for a major new feature such as a new activity will be accepted only if there is consensus among ALL project admins. Any proposal for a new feature that requires extension of MIM schema will be rejected. Any proposal that has serious backward compatibility considerations is unlikely to get accepted. A classic example of this is that in contrast to specification of FIM string functions such as `Mid`, all WAL string functions expect a zero-based index. For any features that you need for your project, but will not make it to the MIMWAL code base in time (or ever), the immediate best way forward is to keep your extension in a separate DLL. See [Custom Activity Development](https://github.com/Microsoft/MIMWAL/wiki/Custom-Activity-Development) Wiki for more information. 

To summarise, the MIMWAL is intended to be a set of building block activities that are generic not specific. Any extension considered to be added to the core library should be applicable in multiple customer scenarios and not limited to a very specific or narrow use case or a specific customer environment.

### Submit an Issue.

You can also submit an issue. Use the [Issues](https://github.com/Microsoft/MIMWAL/issues) tab to create a new issue.


## Contributing to MIMWAL Wiki

To contribute to the MIMWAL wiki, please submit an Issue. Use the [Issues](https://github.com/Microsoft/MIMWAL/issues) tab to create a new issue.

##### Wiki Acceptance

Your edits will be reviewed and accepted if they align with the recommended practices / product architecture and are not in conflict with prior lessons learnt by the community. Any other edits may still be accepted with proper caveats or special considerations called out appropriately.
