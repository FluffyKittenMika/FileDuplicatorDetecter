node {

	stage 'Build'
		bat 'nuget restore DupeDetecter2000.sln'
		bat "\"${tool 'MSBuild'}\" DupeDetecter2000.sln /p:Configuration=Release /p:Platform=\"Any CPU\" /p:ProductVersion=1.0.0.${env.BUILD_NUMBER}"
}
