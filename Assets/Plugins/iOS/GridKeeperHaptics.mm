#import <UIKit/UIKit.h>

extern "C" {

void GK_HapticTick()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
        [generator impactOccurred];
    });
}

void GK_HapticSelection()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        UISelectionFeedbackGenerator *generator = [[UISelectionFeedbackGenerator alloc] init];
        [generator selectionChanged];
    });
}

void GK_HapticError()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        UINotificationFeedbackGenerator *generator = [[UINotificationFeedbackGenerator alloc] init];
        [generator notificationOccurred:UINotificationFeedbackTypeError];
    });
}

void GK_HapticSuccess()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        UINotificationFeedbackGenerator *generator = [[UINotificationFeedbackGenerator alloc] init];
        [generator notificationOccurred:UINotificationFeedbackTypeSuccess];
    });
}

}
