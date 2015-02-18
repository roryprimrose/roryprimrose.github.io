---
title: Running deployment tasks in a TFS lab build
tags : TFS
date: 2012-12-10 14:54:18 +10:00
---

I’ve been having issues running deployment tasks in a TFS build today. The batch file I’m getting it to execute on a target machine just doesn’t seem to find the setup packages in the TFS drop location. I suspected that it was executing the deployment steps using a local account on the target machine. Adding an _echo %username%_ in the script confirmed that this was the case. Even though the target machine is a domain joined machine, local accounts won’t help with getting access to the TFS drop location which is secured by an ACL.

The account used to execute a deployment step is the lab agent service account identity on the target machine.![image][0]

By default this appears to be local system. You need to reconfigure the local Lab Agent service on the target machine in order to have it execute deployment steps under that account.![image][1]

The deployment step now identifies that it is executing under the correct credential. ![image][2]

I still get a deployment step failure, but weirdly the deployment and the build were both successful.

[0]: //blogfiles/image_147.png
[1]: //blogfiles/image_148.png
[2]: //blogfiles/image_149.png
