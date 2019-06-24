
import sys
import os
import re

from functools import partial
from datetime import date

try:
    from github import Github
except ImportError:
    sys.stderr.write("Install 'pip install PyGithub'")
    exit()


me = os.path.dirname(__file__)

source_pattern = re.compile(r'\s*public const string Version = \"(?P<v>[\d\.]+)\";', re.MULTILINE)
source_extension_cs = os.path.join(me, "Conan.VisualStudio", "source.extension.cs")

vsixmanifest_pattern = re.compile(r'\s+<Identity .*Version=\"(?P<v>[\d\.]+)\".*', re.MULTILINE)
source_vsixmanifest_cs = os.path.join(me, "Conan.VisualStudio", "source.extension.vsixmanifest")


def get_current_version():
    v_source_extension = None
    v_source_manifest = None

    # Get version from 'source.extension.cs'
    for line in open(source_extension_cs, "r").readlines():
        m = source_pattern.match(line)
        if m:
            v_source_extension = m.group("v")
    # Get version from 'source.extension.vsixmanifest'
    for line in open(source_vsixmanifest_cs, "r").readlines():
        m = vsixmanifest_pattern.match(line)
        if m:
            v_source_manifest = m.group("v")

    assert v_source_extension == v_source_manifest, "Versions in {!r} and {!r} are different:" \
            " {!r} != {!r}".format(source_extension_cs, source_vsixmanifest_cs, v_source_extension, v_source_manifest)
    return v_source_extension


def set_current_version(version):
    v_source_extension = None
    v_source_manifest = None

    def replace_closure(subgroup, replacement, m):
        if m.group(subgroup) not in [None, '']:
            start = m.start(subgroup)
            end = m.end(subgroup)
            return m.group(0)[:start] + replacement + m.group(0)[end:]

    # Substitute version in 'source.extension.cs'
    lines = []
    for line in open(source_extension_cs, "r").readlines():
        line_sub = source_pattern.sub(partial(replace_closure, "v", version), line)
        lines.append(line_sub)
    with open(source_extension_cs, "w") as f:
        f.write("".join(lines))

    # Substitute version in 'source.extension.vsixmanifest'
    lines = []
    for line in open(source_vsixmanifest_cs, "r").readlines():
        line_sub = vsixmanifest_pattern.sub(partial(replace_closure, "v", version), line)
        lines.append(line_sub)
    with open(source_vsixmanifest_cs, "w") as f:
        f.write("".join(lines))


def write_changelog(version, prs):
    print("*"*20)
    changelog = os.path.join(me, "CHANGELOG.md")
    new_content = []

    changelog_found = False
    version_pattern = re.compile("## [\d\.]+")
    for line in open(changelog, "r").readlines():
        if not changelog_found:
            changelog_found = bool(line.strip() == "# Changelog")
        else:
            if version_pattern.match(line):
                # Add before new content
                new_content.append("## {}\n\n".format(version))
                new_content.append("**{}**\n\n".format(date.today().strftime('%Y-%m-%d')))
                for it in prs:
                    new_content.append("- {}\n".format(it.title))
                new_content.append("\n\n")
        new_content.append(line)

    with open(changelog, "w") as f:
        f.write("".join(new_content))


def get_git_current_branch():
    return os.popen('git rev-parse --abbrev-ref HEAD').read()

def query_yes_no(question, default="yes"):
    valid = {"yes": True, "y": True, "ye": True,
             "no": False, "n": False}
    if default is None:
        prompt = " [y/n] "
    elif default == "yes":
        prompt = " [Y/n] "
    elif default == "no":
        prompt = " [y/N] "
    else:
        raise ValueError("invalid default answer: '%s'" % default)

    while True:
        sys.stdout.write(question + prompt)
        choice = input().lower()
        if default is not None and choice == '':
            return valid[default]
        elif choice in valid:
            return valid[choice]
        else:
            sys.stdout.write("Please respond with 'yes' or 'no' (or 'y' or 'n').\n")

def work_on_release(next_release):
    github_token = os.environ.get("GITHUB_TOKEN")
    if not github_token:
        print(github_token)
        sys.stderr.write("Please, provide a read-only token to access Github using environment variable 'GITHUB_TOKEN'")

    # Find matching milestone
    g = Github(github_token)
    repo = g.get_repo('conan-io/conan-vs-extension')
    open_milestones = repo.get_milestones(state='open')
    for milestone in open_milestones:
        if str(milestone.title) == next_release:
            # Gather pull requests
            prs = [it for it in repo.get_pulls(state="all") if it.milestone == milestone]
            sys.stdout.write("Found {} pull request for this milestone:\n".format(len(prs)))
            for p in prs:
                status = "[!]" if not p.is_merged() else ""
                sys.stdout.write("\t {}\t{}\n".format(status, p.title))
            
            # Gather issues
            issues = [it for it in repo.get_issues(milestone=milestone, state="all")]
            sys.stdout.write("Found {} issues for this milestone:\n".format(len(issues)))
            for issue in issues:
                status = "[!]" if issue.state != "closed" else ""
                sys.stdout.write("\t {}\t{}\n".format(status, issue.title))
            
            # Any open PR or issue?
            if any([not p.is_merged() for p in prs]) or any([issue.state != "closed" for issue in issues]):
                sys.stderr.write("Close all PRs and issues belonging to the milestone before making the release")
                return
            
            # Modify the working directory
            set_current_version(next_release)
            write_changelog(next_release, prs)

            # Commit current and checkout rc branch
            # TODO: May automate all of this.
            os.system("git add CHANGELOG.md")
            os.system("git add Conan.VisualStudio/source.extension.cs")
            os.system("git add Conan.VisualStudio/source.extension.vsixmanifest")

            if query_yes_no("Commit change to 'dev' branch"):
                os.system('git commit -m "close milestone {}"'.format(next_release))
                os.system('git checkout -b rc-{}'.format(next_release))

                sys.stdout.write("Now commit this branch and make the PR to master")

            break
    else:
        sys.stderr.write("No milestone matching version {!r}. Open milestones found were '{}'\n".format(next_release, "', '".join([it.title for it in open_milestones])))


if __name__ == "__main__":
    current_branch = get_git_current_branch()
    if current_branch != "dev":
        sys.stderr.write("Move to the 'dev' branch to work with this tool\n")
        exit()

    v = get_current_version()
    sys.stdout.write("Current version is {!r}\n".format(v))

    major, minor, _ = v.split(".")
    next_release = ".".join([major, str(int(minor)+1), "0"])
    if query_yes_no("Next version will be {!r}".format(next_release)):
        work_on_release(next_release)
        
    else:
        sys.stdout.write("Sorry, I cannot help you then...")
