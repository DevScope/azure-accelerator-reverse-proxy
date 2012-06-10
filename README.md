# Windows Azure Accelerator for Reverse Proxy (using ARR) #


## About
The Windows Azure Accelerator for Reverse Proxy makes it quick and easy for you to deploy one or more ARR websites across multiple Web Role instances using Web Deploy. The accelerator includes a Visual Studio project template that creates a Windows Azure web role to host one or more websites. Once you have deployed this Web Role you can deploy your websites to Windows Azure using Web Deploy. Deployments to Windows Azure using Web Deploy take only about 30 seconds. Additionally, this tool will work with roles that have a single or many instances so you can scale up to handle as much traffic as you need.


## When Should I Use This? ##
Before you download and use this accelerator it is important to understand what it is and how it should be used. First, remember that the accelerator is sample code. It is not part of the Windows Azure Platform and is not a supported project. It is a tool that you can use if it fits your needs. The project is open source so you can modify it, fix bugs, or rip it apart and use pieces in any way that meets your needs. Second, the accelerator was designed to simplify certain types of deployments on Windows Azure. The accelerator was built to help developers deploy and host multiple websites on a single set of instances. The accelerator works best with sites that are small in size and simple in scope. For these scenarios the accelerator has been working well for many people. Now, if you are building a high performance, high scale site that has lots of complexity, lots of static content, or other similar scenarios this accelerator probably isn't for you.

## Documentation ##
* [Home](wiki)
* [Setup and Configuration](Setup-and-Configuration)
* [Deploying](Deploying)
* [Changing Portal Credentials](portal-credentials)
* [Modifying Service Endpoints](service-endpoints)
* [Known Issues](known-issues)