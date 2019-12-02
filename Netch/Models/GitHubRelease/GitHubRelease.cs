namespace Netch.Models.GitHubRelease
{
    public class GitHubRelease
    {
        private readonly string _owner;
        private readonly string _repo;

        public string AllReleaseUrl => $@"https://api.github.com/repos/{_owner}/{_repo}/releases";

        public GitHubRelease(string owner, string repo)
        {
            _owner = owner;
            _repo = repo;
        }
    }
}
