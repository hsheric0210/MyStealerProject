#include "pch.h"
#include "memory_chk.h"
#include "skCrypter.h"
#include "safe_calls.h"
#include "GetProcAddressSilent.h"

bool mem_ntqueryvirtualmemory()
{
#ifndef _WIN64
    auto pfnNtQueryVirtualMemory = (TNtQueryVirtualMemory)safeGetProcAddress((HMODULE)GetModu1eH4ndle(skCrypt(L"ntdll.dll")), skCrypt("NtQueryVirtualMemory"));

    NTSTATUS status;
    PBYTE pMem = nullptr;
    DWORD dwMemSize = 0;

    do
    {
        dwMemSize += 0x1000;
        pMem = (PBYTE)_malloca(dwMemSize);
        if (!pMem)
            return false;

        memset(pMem, 0, dwMemSize);
        status = pfnNtQueryVirtualMemory(GetCurrentProcess(), NULL, MemoryWorkingSetList, pMem, dwMemSize, NULL);
    } while (status == STATUS_INFO_LENGTH_MISMATCH);

    PMEMORY_WORKING_SET_LIST pWorkingSet = (PMEMORY_WORKING_SET_LIST)pMem;
    for (ULONG i = 0; i < pWorkingSet->NumberOfPages; i++)
    {
        DWORD dwAddr = pWorkingSet->WorkingSetList[i].VirtualPage << 0x0C;
        DWORD dwEIP = 0;
        __asm
        {
            push eax
            call $ + 5
            pop eax
            mov dwEIP, eax
            pop eax
        }

        if (dwAddr == (dwEIP & 0xFFFFF000))
            return (pWorkingSet->WorkingSetList[i].Shared == 0) || (pWorkingSet->WorkingSetList[i].ShareCount == 0);
    }
#endif // _WIN64
    return false;
}

#ifndef _WIN64
static __declspec(naked) int code_checksum_test()
{
    __asm
    {
        push edx
        mov edx, 0
        mov eax, 10
        mov ecx, 2
        div ecx
        pop edx
        ret
    }
}

size_t calculate_function_size(PVOID pFunc)
{
    PBYTE pMem = (PBYTE)pFunc;
    size_t nFuncSize = 0;
    do
    {
        ++nFuncSize;
    } while (*(pMem++) != 0xC3);
    return nFuncSize;
}

unsigned bit_reverse(unsigned x)
{
    x = ((x & 0x55555555) << 1) | ((x >> 1) & 0x55555555);
    x = ((x & 0x33333333) << 2) | ((x >> 2) & 0x33333333);
    x = ((x & 0x0F0F0F0F) << 4) | ((x >> 4) & 0x0F0F0F0F);
    x = (x << 24) | ((x & 0xFF00) << 8) | ((x >> 8) & 0xFF00) | (x >> 24);
    return x;
}

unsigned int crc32(unsigned char *message, int size)
{
    unsigned int byte, crc = 0xFFFFFFFF;
    for (int i = 0; i < size; i++)
    {
        byte = message[i];
        byte = bit_reverse(byte);
        for (int j = 0; j <= 7; j++)
        {
            if ((int)(crc ^ byte) < 0)
                crc = (crc << 1) ^ 0x04C11DB7;
            else
                crc = crc << 1;
            byte = byte << 1;
        }
    }
    return bit_reverse(~crc);
}

PVOID mem_code_checksum_func_addr;
size_t mem_code_checksum_func_size;
UINT32 mem_code_checksum_checksum;

void mem_code_checksum_init()
{
    code_checksum_test();
    mem_code_checksum_func_addr = &code_checksum_test;
    mem_code_checksum_func_size = calculate_function_size(mem_code_checksum_func_addr);
    mem_code_checksum_checksum = crc32((PBYTE)mem_code_checksum_func_addr, mem_code_checksum_func_size);
    DWORD Checksum = crc32((PBYTE)mem_code_checksum_func_addr, mem_code_checksum_func_size);
}

bool mem_code_checksum_check()
{
    return crc32((PBYTE)mem_code_checksum_func_addr, mem_code_checksum_func_size) != mem_code_checksum_checksum;
}
#else
bool mem_code_checksum_check()
{
    return false;
}
#endif

// https://github.com/LordNoteworthy/al-khaser/blob/master/al-khaser/AntiDebug/MemoryBreakpoints_PageGuard.cpp
bool mem_pageguard()
{
    SYSTEM_INFO SystemInfo = { 0 };
    DWORD OldProtect = 0;
    PVOID pAllocation = NULL;

    GetSystemInfo(&SystemInfo);

    pAllocation = VirtualAlloc(NULL, SystemInfo.dwPageSize, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    if (pAllocation == NULL)
        return FALSE;

    RtlFillMemory(pAllocation, 1, 0xC3);

    if (VirtualProtect(pAllocation, SystemInfo.dwPageSize, PAGE_EXECUTE_READWRITE | PAGE_GUARD, &OldProtect) == 0)
        return FALSE;

    __try
    {
        ((void(*)())pAllocation)(); // Exception or execution, which shall it be :D?
    }
    __except (GetExceptionCode() == STATUS_GUARD_PAGE_VIOLATION ? EXCEPTION_EXECUTE_HANDLER : EXCEPTION_CONTINUE_SEARCH)
    {
        VirtualFree(pAllocation, 0, MEM_RELEASE);
        return FALSE;
    }

    VirtualFree(pAllocation, 0, MEM_RELEASE);
    return TRUE;
}
