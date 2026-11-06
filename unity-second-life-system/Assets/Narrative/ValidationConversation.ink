// Validation fixture. This is not story canon.
VAR validation_marker = 0

EXTERNAL can_offer_help()
EXTERNAL record_choice(choice_id)

-> meeting

=== meeting ===
A classmate asks for help before the next lesson.
{ can_offer_help():
    * [Offer help] -> offered
}
* [Keep the evening free] -> declined

=== offered ===
~ temp accepted = record_choice("offer_help")
{ accepted:
    ~ validation_marker = 1
    You agree to help.
}
{ not accepted:
    The offer does not take effect.
}
-> END

=== declined ===
~ temp accepted = record_choice("decline_help")
{ accepted:
    ~ validation_marker = 2
    You keep the evening free.
}
{ not accepted:
    The refusal does not take effect.
}
-> END
