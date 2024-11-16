def as_dict(real_dict_row):
    """
    Transform a RealDictRow to a plain dictionary.

    Args:
        real_dict_row (RealDictRow): The RealDictRow to transform.

    Returns:
        dict: A plain dictionary representation of the RealDictRow.
    """
    if isinstance(real_dict_row, dict):
        return dict(real_dict_row)  # If it's already a dict, return it
    return {key: value for key, value in real_dict_row.items()}
def fetchall_as_dict(cursor):
    """
    Converts the cursor result into a list of dictionaries.

    Args:
        cursor: The database cursor containing the result set.

    Returns:
        list: A list of dictionaries where each row is represented as a dict.
    """
    # Get column names from the cursor description
    columns = [col[0] for col in cursor.description]
    
    # Fetch all rows and convert each row into a dictionary
    return [as_dict(row) for row in cursor.fetchall()]