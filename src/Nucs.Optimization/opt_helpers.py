import json
import clr
from System.Collections.Generic import SortedDictionary
import System


def scoreWrapper(func, names, maximize):
    def minimize_wrapper(*args):
        return func(unbox_params(names, args[0]))

    def maximize_wrapper(*args):
        return -func(unbox_params(names, args[0]))  # negate the score to minimize

    if maximize:
        return maximize_wrapper
    else:
        return minimize_wrapper


def unbox_params(names, result):
    tupleType = System.Tuple[System.String, System.Object]
    listType = System.Collections.Generic.List[tupleType]
    unboxed = listType(len(names))
    for key, value in zip(names, result):
        if not isinstance(key, str):
            key = str(key)

        if hasattr(value, 'dtype'):
            value = value.item()

        unboxed.Add(tupleType(key, value))

    return unboxed


def unbox_params_dictionary(names, result):
    managedDict = SortedDictionary[System.String, System.Object]()
    for key, value in zip(names, result):
        if not isinstance(key, str):
            key = str(key)

        if hasattr(value, 'dtype'):
            value = value.item()

        managedDict[key] = value

    return managedDict


# A dictionary capable of quick conversion to C# Dictionary<string, object>
class netdict(dict):
    def asManaged(self, neededKeyType=None, neededValueType=None):
        return unbox_params(self.keys(), self.values())

    def __init__(self, *args, **kwargs) -> None:
        super().__init__(**kwargs)
        for key, value in args:
            self[key] = value


# Helps with conversion of PyObject into a json
def asJson(params):
    return json.loads(params)
