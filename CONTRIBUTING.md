# Contributing to LLMUnity

:+1: :tada: :heart: Thanks for your interest! :heart: :tada: :+1:

The following is a set of guidelines for contributing to [LLMUnity](https://github.com/undreamai/LLMUnity). These are just guidelines, not rules. Use your best judgment, and
feel free to propose changes to this document in a pull request.

#### Table Of Contents

[How Can I Contribute?](#how-can-i-contribute)
  * [Code of Conduct](#code-of-conduct)
  * [Set up your dev environment](#set-up-your-dev-environment)
  * [Reporting Bugs](#reporting-bugs)
  * [Suggesting Enhancements](#suggesting-enhancements)
  * [Good First Issue](#good-first-issue)
  * [Issue and Pull Request Labels](#issue-and-pull-request-labels)


## How Can I Contribute?

### Code of Conduct

This project adheres to the Contributor Covenant [code of conduct](CODE_OF_CONDUCT.md).
By participating, you are expected to uphold this code.

### Set up your dev environment


1. Fork the repo.
2. Clone your forked repo into a Unity project's `Assets`.
3. Create a symbolic link to `Samples~`, for example with:
  ```bash
  cd Assets && ln -s ./LLMUnity/Samples~ ./Samples 
  ```
4. Add the package to your projects libraries `Packages/manifest.json`:
  ```json
  "ai.undream.llm": "file:path/to/project/Assets/LLMUnity",
  ```
5. Create a topic branch from where you want to base your work.
Name your branch prefixed with an appropriate [label](https://github.com/undreamai/LLMUnity/labels), following the naming convention `enhancement/*`, `bug/*`, `documentation/*`, etc. Make commits of logical units.
6. Set up pre-commit hooks with `sh ./.github/setup.sh`


### Reporting Bugs

This section guides you through submitting a bug report for LLMUnity.
Following these guidelines helps maintainers and the community understand your
report :pencil:, reproduce the behavior :computer:, and find related
reports :mag_right:.

Before creating bug reports, please check [this section](#before-submitting-a-bug-report)
as you might find out that you don't need to create one. When you are creating
a bug report, please [include as many details as possible](#how-do-i-submit-a-good-bug-report) as it helps us resolve issues faster.

#### Before Submitting A Bug Report

**Perform a [cursory search](https://github.com/undreamai/LLMUnity/labels/bug)**
to see if the problem has already been reported. If it does exist, add a
[reaction](https://help.github.com/articles/about-discussions-in-issues-and-pull-requests/#reacting-to-ideas-in-issues-and-pull-requests)
to the issue to indicate this is also an issue for you, and add a
comment to the existing issue if there is extra information you can contribute.

#### How Do I Submit A (Good) Bug Report?

Bugs are tracked as [GitHub issues](https://guides.github.com/features/issues/).

Simply create an issue on the [LLMUnity issue tracker](https://github.com/undreamai/LLMUnity/issues), choose the appropriate provided issue template and fill it out.

The information we are interested in includes:

 - details about your environment - which build, which operating system
 - details about reproducing the issue - what steps to take, what happens, how
   often it happens
 - other relevant information - log files, screenshots, etc.

### Suggesting Enhancements

This section guides you through submitting an enhancement suggestion for
LLMUnity, including completely new features and minor improvements to
existing functionality. Following these guidelines helps maintainers and the
community understand your suggestion :pencil: and find related suggestions
:mag_right:.

Before creating enhancement suggestions, please check [this section](#before-submitting-an-enhancement-suggestion)
as you might find out that you don't need to create one. When you are creating
an enhancement suggestion, please [include as many details as possible](#how-do-i-submit-a-good-enhancement-suggestion).

#### Before Submitting An Enhancement Suggestion

**Perform a [cursory search](https://github.com/undreamai/LLMUnity/labels/enhancement)**
to see if the enhancement has already been suggested. If it has, add a
:thumbsup: to indicate your interest in it, or comment if there is additional
information you would like to add.

#### How Do I Submit A (Good) Enhancement Suggestion?

Enhancement suggestions are tracked as [GitHub issues](https://guides.github.com/features/issues/).

Simply create an issue on the [LLMUnity issue tracker](https://github.com/undreamai/LLMUnity/issues), choose the appropriate provided issue template and fill it out and provide the following information:

* **Use a clear and descriptive title** for the issue to identify the
  suggestion.
* **Provide a step-by-step description of the suggested enhancement** in as
  much detail as possible. This additional context helps the maintainers to
  understand the enhancement from your perspective
* **Explain why this enhancement would be useful** to LLMUnity users.
* **Include screenshots and animated GIFs** if relevant to help you demonstrate
  the steps or point out the part of LLMUnity which the suggestion is
  related to. You can use [this tool](http://www.cockos.com/licecap/) to record
  GIFs on macOS and Windows.
* **List some other applications where this enhancement exists, if applicable.**

### Good First Issue

We'll identify enhancements or bugs that can be categorized as tasks that:

 - have low impact, or have a known workaround
 - should be fixed
 - have a narrow scope and/or easy reproduction steps
 - can be worked on independent of other tasks

These issues will be labelled as [`good-first-issue`](https://github.com/undreamai/LLMUnity/labels/good%20first%20issue)
in the repository. If you are interested in contributing to the project, please
comment on the issue to let the maintainers (and community) know you are
interested in picking this up.

### Issue and Pull Request Labels

See [this page](https://github.com/undreamai/LLMUnity/labels) for the list of the labels we use to help us track and manage issues and pull requests.




