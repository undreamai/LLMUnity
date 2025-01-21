import os
import sys


def get_classname(line):
    if ' class ' not in line or ':' not in line:
        return None, None
    classParts = line.strip().split(' ')
    delimInd = classParts.index(':')
    className = classParts[delimInd - 1]
    parentName = classParts[delimInd + 1]
    return className, parentName

def find_eligible_classes(file_paths):
    child_classes = {}
    for file_path in file_paths:
        with open(file_path, 'r') as file:
            lines = file.readlines()
            for line in lines:
                className, parentName = get_classname(line)
                if className is not None:
                    child_classes[parentName] = child_classes.get(parentName, []) + [className]

    ret_classes = []
    check_classes = ['MonoBehaviour']
    while len(check_classes) > 0:
        check_class = check_classes.pop()
        if check_class in ret_classes:
            continue
        if check_class != 'MonoBehaviour':
            ret_classes.append(check_class)
        check_classes += child_classes.get(check_class, [])
    return ret_classes
    


def add_tooltips_to_unity_file(file_path, allowed_classes):
    # Read the content of the file
    with open(file_path, 'r') as file:
        lines = file.readlines()

    # Initialize variables
    updated_lines = []
    in_summary = False
    allowed_class = False
    summary_text = ""

    for line in lines:
        stripped_line = line.strip()
        className, __ = get_classname(line)
        if className is not None:
            allowed_class = className in allowed_classes

        if allowed_class:
            if '<summary>' in stripped_line:
                in_summary = True
                summary_text = ''
            
            if in_summary:
                if summary_text != "": summary_text += ' '
                summary_text += stripped_line.replace("///", "").replace("<summary>", "").replace("</summary>", "").strip()
            
            if '</summary>' in stripped_line:
                in_summary = False

            if 'Tooltip' in stripped_line:
                if ('Tooltip: ignore' not in stripped_line):
                    continue

            include_terms = ['public', ';']
            exclude_terms = ['{', 'static', 'abstract']
            if all([x in stripped_line for x in include_terms]) and not any([x in stripped_line for x in exclude_terms]):
                if summary_text != '':
                    num_spaces = len(line) - len(line.lstrip())
                    tooltip = ''.join([' '] * num_spaces + [f'[Tooltip("{summary_text}")]', '\n'])
                    updated_lines.append(tooltip)
                    summary_text = ''

            if not in_summary and ('{' in stripped_line or '}' in stripped_line):
                summary_text = ''

        # Add the current line to the updated lines
        updated_lines.append(line)

    # Write the updated content back to the file
    with open(file_path, 'w') as file:
        file.writelines(updated_lines)





if __name__ == '__main__':
    # Find all .cs files
    search_directory = 'Runtime'
    cs_files = []
    for root, _, files in os.walk(search_directory):
        for file in files:
            if file.endswith(".cs"):
                cs_files.append(os.path.join(root, file))

    classes = find_eligible_classes(cs_files)
    for file in cs_files:
        add_tooltips_to_unity_file(file, classes)
