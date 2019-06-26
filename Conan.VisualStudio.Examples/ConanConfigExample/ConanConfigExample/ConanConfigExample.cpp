#include <iostream>
#include <fmt/format.h>
#include <fmt/printf.h>

int main()
{
    fmt::printf("Using fmt version '%d'\n", FMT_VERSION);
    #ifdef FMT_STRING_ALIAS
        // The CustomDebug configuration is mapped to the profile 'custom_config.profile'
        //  where we are setting the value of the option 'fmt:with_fmt_alias=True'
        fmt::printf("Using FMT_STRING_ALIAS\n");
    #endif
}
